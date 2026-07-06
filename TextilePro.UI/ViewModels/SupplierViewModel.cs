using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TextilePro.Core.DbContext;
using TextilePro.Core.Models;

using TextilePro.Core.Services;

#nullable enable

namespace TextilePro.UI.ViewModels;

public partial class SupplierViewModel : ObservableObject
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;
    private readonly string _currentUsername;

    [ObservableProperty]
    private ObservableCollection<Supplier> _suppliers = new();

    [ObservableProperty]
    private Supplier? _selectedSupplier;

    [ObservableProperty]
    private ObservableCollection<SupplierContact> _contacts = new();

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public SupplierViewModel(AppDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;

        var session = Application.Current.Properties["Session"] as dynamic;
        _currentUsername = session?.Username ?? "Unknown";

        _ = LoadSuppliersAsync();
    }

    private ObservableCollection<Supplier> _allSuppliers = new();

    private async Task LoadSuppliersAsync()
    {
        var list = await _context.Suppliers
            .Include(s => s.Contacts)
            .OrderBy(s => s.Name)
            .ToListAsync();

        _allSuppliers = new ObservableCollection<Supplier>(list);
        ApplyFilter();

        if (Suppliers.Any())
            SelectedSupplier = Suppliers.First();
        else
            NewSupplier();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Suppliers = new ObservableCollection<Supplier>(_allSuppliers);
        }
        else
        {
            var filtered = _allSuppliers
                .Where(s => s.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                .ToList();
            Suppliers = new ObservableCollection<Supplier>(filtered);
        }
    }

    partial void OnSelectedSupplierChanged(Supplier? oldValue, Supplier? newValue)
    {
        if (newValue != null)
        {
            Contacts = new ObservableCollection<SupplierContact>(newValue.Contacts);
            IsEditing = true;
        }
        else
        {
            Contacts.Clear();
            IsEditing = false;
        }
    }

    [RelayCommand]
    private void NewSupplier()
    {
        SelectedSupplier = new Supplier
        {
            CreatedDate = DateTime.Now,
            ModifiedDate = DateTime.Now
        };
        Contacts = new ObservableCollection<SupplierContact>();
        IsEditing = false;
    }

    [RelayCommand]
    private void EditSupplier(Supplier supplier)
    {
        if (supplier == null) return;
        SelectedSupplier = supplier;
        // OnSelectedSupplierChanged will populate Contacts and set IsEditing
    }

    [RelayCommand]
    private async Task SaveSupplierAsync()
    {
        try
        {
            if (SelectedSupplier == null) return;

            // Validate Name
            if (string.IsNullOrWhiteSpace(SelectedSupplier.Name))
            {
                MessageBox.Show("Supplier Name is required.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

        // Check duplicate name (excluding current entity)
        var exists = await _context.Suppliers
            .AnyAsync(s => s.Name == SelectedSupplier.Name && s.Id != SelectedSupplier.Id);
        if (exists)
        {
            MessageBox.Show("A supplier with this name already exists.", "Duplicate Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

            var isNewSupplier = SelectedSupplier.Id == 0;

            // Set dates
            if (isNewSupplier)
            {
                SelectedSupplier.CreatedDate = DateTime.Now;
                _context.Suppliers.Add(SelectedSupplier);
            }

            SelectedSupplier.ModifiedDate = DateTime.Now;

            if (isNewSupplier)
            {
                // New supplier: attach contacts via navigation so EF sets FK correctly
                foreach (var contact in Contacts)
                {
                    contact.Supplier = SelectedSupplier;
                    _context.SupplierContacts.Add(contact);
                }
            }
            else
            {
                // Keep existing contacts in place, remove only deleted ones, and add only new ones.
                var existingContacts = await _context.SupplierContacts
                    .Where(c => c.SupplierId == SelectedSupplier.Id)
                    .ToListAsync();

                var contactIdsInUi = Contacts
                    .Where(c => c.Id != 0)
                    .Select(c => c.Id)
                    .ToHashSet();

                var contactsToRemove = existingContacts
                    .Where(c => !contactIdsInUi.Contains(c.Id))
                    .ToList();

                if (contactsToRemove.Count > 0)
                {
                    _context.SupplierContacts.RemoveRange(contactsToRemove);
                }

                foreach (var contact in Contacts)
                {
                    contact.SupplierId = SelectedSupplier.Id;

                    if (contact.Id == 0)
                    {
                        _context.SupplierContacts.Add(contact);
                    }
                }
            }

            await _context.SaveChangesAsync();

        // Audit
        var action = SelectedSupplier.Id == 0 ? "Added" : "Updated";
        _auditService.Log(
            _currentUsername,
            IsAdmin() ? "Admin" : "User",
            $"{action} supplier: {SelectedSupplier.Name} with {Contacts.Count} contacts",
            "Suppliers"
        );

            MessageBox.Show($"Supplier '{SelectedSupplier.Name}' saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            // Refresh list and try to preserve selection
            await LoadSuppliersAsync();
            if (!string.IsNullOrEmpty(SelectedSupplier?.Name))
            {
                var reloaded = _allSuppliers.FirstOrDefault(s => s.Id == SelectedSupplier.Id);
                if (reloaded != null)
                    SelectedSupplier = reloaded;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving supplier:\n{ex}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task DeleteSupplierAsync(Supplier supplier)
    {
        try
        {
            if (supplier == null) return;
            var result = MessageBox.Show($"Are you sure you want to delete '{supplier.Name}'?\nAll related data (contacts, chemicals, evaluations) will also be removed.",
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            _context.Suppliers.Remove(supplier);
            await _context.SaveChangesAsync();

            _auditService.Log(
                _currentUsername,
                IsAdmin() ? "Admin" : "User",
                $"Deleted supplier: {supplier.Name}",
                "Suppliers"
            );

            await LoadSuppliersAsync();
            NewSupplier();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error deleting supplier:\n{ex}", "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Helper to add a contact row
    [RelayCommand]
    private void AddContact()
    {
        Contacts.Add(new SupplierContact
        {
            ContactName = "",
            Email = "",
            Phone = "",
            Website = ""
        });
    }

    // Helper to remove a contact row
    [RelayCommand]
    private void RemoveContact(SupplierContact contact)
    {
        if (contact == null) return;
        Contacts.Remove(contact);
    }

    // Check if current user is Admin
    private bool IsAdmin()
    {
        var session = Application.Current.Properties["Session"] as dynamic;
        return session?.IsAdmin ?? false;
    }
}