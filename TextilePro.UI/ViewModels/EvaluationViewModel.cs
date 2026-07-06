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

namespace TextilePro.UI.ViewModels;

public partial class EvaluationViewModel : ObservableObject
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IFileService _fileService;
    private readonly string _currentUsername;
    private int _evaluationLoadVersion;

    public ObservableCollection<string> SelfDeclarationOptions { get; } = new()
    {
        "Available",
        "Not Available",
        "Pending"
    };

    // Question definitions (as per ZDHC)
    public ObservableCollection<EvaluationQuestion> Questions { get; } = new()
    {
        new EvaluationQuestion(1, "Details of organisation", "Location of the manufacturing site",
            new[] { "Manufactured (2 pts)", "Traded, known origin (1 pt)", "Traded, unknown origin (0 pt)" },
            new[] { 2, 1, 0 }),
        new EvaluationQuestion(2, "Technical person", "Name of technical person/QC Head",
            new[] { "Tech person/QC head name is known (2 pts)", "Commercial person is known (1 pt)", "No proper person is known (0 pt)" },
            new[] { 2, 1, 0 }),
        new EvaluationQuestion(3, "Traceability", "System of traceability of commodity chemical source",
            new[] { "System with documentation (2 pts)", "System without backup (1 pt)", "No traceability (0 pt)" },
            new[] { 2, 1, 0 }),
        new EvaluationQuestion(4, "Consistency", "Quality consistency of commodity chemicals",
            new[] { "No changes for past batches (2 pts)", "Variation >10% deliveries (1 pt)", "Variation >25% deliveries (0 pt)" },
            new[] { 2, 1, 0 }),
        new EvaluationQuestion(5, "Supply reliability", "Ability to supply ordered quantity",
            new[] { "Immediate full lot (2 pts)", "Delivered in several lots (1 pt)", "Not fulfilled/spread out (0 pt)" },
            new[] { 2, 1, 0 }),
        new EvaluationQuestion(6, "Technical Knowledge", "Qualified team for quality decisions",
            new[] { "Internal qualified QC team (2 pts)", "Relies on external decision (1 pt)", "No testing carried out (0 pt)" },
            new[] { 2, 1, 0 }),
        new EvaluationQuestion(7, "Quality Lab", "Quality testing laboratory",
            new[] { "In-house facility (2 pts)", "Third-party testing (1 pt)", "No testing done (0 pt)" },
            new[] { 2, 1, 0 }),
        new EvaluationQuestion(8, "ZDHC MRSL Awareness", "Knowledge of ZDHC MRSL",
            new[] { "Fully aware with latest version (2 pts)", "Somewhat aware, no update (1 pt)", "No awareness (0 pt)" },
            new[] { 2, 1, 0 }),
        new EvaluationQuestion(9, "Rejection Rate", "Rejection rate from different sources",
            new[] { "Less than 10% (2 pts)", "Between 10-20% (1 pt)", "More than 20% (0 pt)" },
            new[] { 2, 1, 0 }),
        new EvaluationQuestion(10, "Complaint Handling", "System to carry out complaints",
            new[] { "ISO system (2 pts)", "No system but handled (1 pt)", "Oral only (0 pt)" },
            new[] { 2, 1, 0 }),
        new EvaluationQuestion(11, "SDS Information", "Can provide GHS compliant SDS",
            new[] { "GHS-SDS regularly (2 pts)", "SDS not GHS compliant (1 pt)", "No SDS provided (0 pt)" },
            new[] { 2, 1, 0 }),
        new EvaluationQuestion(12, "Third-party Certifications", "Third-party Certification for MRSL testing",
            new[] { "Report submitted regularly (2 pts)", "Report intermittent (1 pt)", "No report ever (0 pt)" },
            new[] { 2, 1, 0 })
    };

    [ObservableProperty]
    private ObservableCollection<Supplier> _suppliers = new();

    [ObservableProperty]
    private Supplier? _selectedSupplier;

    [ObservableProperty]
    private Evaluation? _currentEvaluation = new();

    [ObservableProperty]
    private ObservableCollection<EvaluationDocument> _documents = new();

    [ObservableProperty]
    private string _selfDeclarationStatus = "Not Available";

    [ObservableProperty]
    private DateTime _evaluationDate = DateTime.Now;

    [ObservableProperty]
    private int _totalScore;

    [ObservableProperty]
    private string _classification = string.Empty;

    [ObservableProperty]
    private string _classificationAction = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEditing;

    [ObservableProperty]
    private string _createdDateDisplay = string.Empty;

    [ObservableProperty]
    private string _modifiedDateDisplay = string.Empty;

    [ObservableProperty]
    private ObservableCollection<EvaluationRecord> _evaluationRecords = new();

    public EvaluationViewModel(AppDbContext context, IAuditService auditService, IFileService fileService)
    {
        _context = context;
        _auditService = auditService;
        _fileService = fileService;

        var session = Application.Current.Properties["Session"] as dynamic;
        _currentUsername = session?.Username ?? "Unknown";

        StatusMessage = "Loading evaluation data...";

        // Recalculate score when any answer changes
        Questions.CollectionChanged += (s, e) => CalculateTotalScore();
        foreach (var q in Questions)
            q.PropertyChanged += (s, e) => CalculateTotalScore();
    }

    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            await LoadSuppliersAsync();
            await LoadEvaluationRecordsAsync();

            if (Suppliers.Count == 0)
            {
                StatusMessage = "No suppliers found yet. Add a supplier to start evaluations.";
                NewEvaluation();
                return;
            }

            if (SelectedSupplier == null)
            {
                SelectedSupplier = Suppliers.FirstOrDefault();
            }
        }
        finally
        {
            IsLoading = false;
        }

        if (SelectedSupplier != null)
        {
            _evaluationLoadVersion++;
            await LoadEvaluationForSupplierAsync(SelectedSupplier, _evaluationLoadVersion);
        }
    }

    partial void OnSelectedSupplierChanged(Supplier? value)
    {
        if (IsLoading)
            return;

        _evaluationLoadVersion++;
        NewEvaluation();

        if (value != null)
        {
            var requestVersion = _evaluationLoadVersion;
            _ = LoadEvaluationForSupplierAsync(value, requestVersion);
        }
    }

    private void CalculateTotalScore()
    {
        TotalScore = Questions.Sum(q => q.SelectedScore < 0 ? 0 : q.SelectedScore);
        UpdateClassification();
    }

    private void UpdateClassification()
    {
        if (TotalScore >= 10 && TotalScore <= 24)
        {
            Classification = "A";
            ClassificationAction = "Continue to purchase - High trust, low risk";
        }
        else if (TotalScore >= 5 && TotalScore <= 9)
        {
            Classification = "B";
            ClassificationAction = "Review & initiate improvements - Medium risk";
        }
        else
        {
            Classification = "C";
            ClassificationAction = "Consider discontinuing - High risk";
        }
    }

    [RelayCommand]
    private async Task LoadSuppliersAsync()
    {
        Suppliers = new ObservableCollection<Supplier>(
            await _context.Suppliers.OrderBy(s => s.Name).ToListAsync()
        );
    }

    [RelayCommand]
    private async Task LoadEvaluationRecordsAsync()
    {
        var records = await _context.Evaluations
            .Include(e => e.Supplier)
            .OrderByDescending(e => e.CreatedDate)
            .ToListAsync();

        EvaluationRecords = new ObservableCollection<EvaluationRecord>(
            records.Select(e => new EvaluationRecord
            {
                SupplierName = e.Supplier?.Name ?? "Unknown",
                EvaluationDate = e.EvaluationDate.ToString("dd-MM-yyyy"),
                Score = e.Score,
                Class = GetClassFromScore(e.Score),
                SelfDeclaration = e.SelfDeclarationStatus,
                EvaluationId = e.Id,
                Action = "View/Edit"
            })
        );
    }

    private string GetClassFromScore(int score)
    {
        if (score >= 10 && score <= 24) return "A";
        if (score >= 5 && score <= 9) return "B";
        return "C";
    }

    private async Task LoadEvaluationForSupplierAsync(Supplier supplier, int requestVersion)
    {
        if (supplier == null)
        {
            NewEvaluation();
            return;
        }

        IsLoading = true;
        try
        {
            var evaluation = await _context.Evaluations
                .Include(e => e.Answers)
                .Include(e => e.Documents)
                .FirstOrDefaultAsync(e => e.SupplierId == supplier.Id);

            if (requestVersion != _evaluationLoadVersion)
                return;

            if (evaluation == null)
            {
                NewEvaluation();
                return;
            }

            // Load evaluation
            CurrentEvaluation = evaluation;
            EvaluationDate = evaluation.EvaluationDate;
            SelfDeclarationStatus = evaluation.SelfDeclarationStatus;
            CreatedDateDisplay = evaluation.CreatedDate.ToString("dd-MM-yyyy HH:mm");
            ModifiedDateDisplay = evaluation.ModifiedDate.ToString("dd-MM-yyyy HH:mm");
            IsEditing = true;

            // Load documents
            Documents = new ObservableCollection<EvaluationDocument>(evaluation.Documents);

            // Set answers
            foreach (var q in Questions)
            {
                var answer = evaluation.Answers.FirstOrDefault(a => a.QuestionId == q.Id);
                q.SelectedScore = answer?.SelectedScore ?? -1; // -1 means not selected
            }

            CalculateTotalScore();
            StatusMessage = $"Loaded evaluation for {supplier.Name}";
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error loading evaluation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void NewEvaluation()
    {
        CurrentEvaluation = new Evaluation
        {
            CreatedDate = DateTime.Now,
            ModifiedDate = DateTime.Now
        };
        EvaluationDate = DateTime.Now;
        SelfDeclarationStatus = SelfDeclarationOptions.First();
        Documents.Clear();
        CreatedDateDisplay = "New";
        ModifiedDateDisplay = "New";
        IsEditing = false;
        foreach (var q in Questions)
            q.SelectedScore = -1;
        TotalScore = 0;
        StatusMessage = "New evaluation form ready.";
        Classification = string.Empty;
        ClassificationAction = string.Empty;
    }

    [RelayCommand]
    private void NewEvaluationCommand()
    {
        if (SelectedSupplier == null)
        {
            MessageBox.Show("Please select a supplier first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _evaluationLoadVersion++;
        NewEvaluation();
    }

    [RelayCommand]
    private async Task SaveEvaluationAsync()
    {
        if (SelectedSupplier == null)
        {
            MessageBox.Show("Please select a supplier.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Validate: all questions must be answered
        var unanswered = Questions.Where(q => q.SelectedScore == -1).ToList();
        if (unanswered.Any())
        {
            var qNumbers = string.Join(", ", unanswered.Select(q => q.Id));
            MessageBox.Show($"Please answer all questions (missing: {qNumbers}).", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsLoading = true;
        try
        {
            bool isNew = CurrentEvaluation?.Id == 0 || CurrentEvaluation == null;
            using var transaction = await _context.Database.BeginTransactionAsync();

            if (isNew)
            {
                CurrentEvaluation = new Evaluation
                {
                    SupplierId = SelectedSupplier.Id,
                    EvaluationDate = EvaluationDate,
                    SelfDeclarationStatus = SelfDeclarationStatus,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now,
                    Score = TotalScore
                };
                _context.Evaluations.Add(CurrentEvaluation);
            }
            else
            {
                CurrentEvaluation.EvaluationDate = EvaluationDate;
                CurrentEvaluation.SelfDeclarationStatus = SelfDeclarationStatus;
                CurrentEvaluation.ModifiedDate = DateTime.Now;
                CurrentEvaluation.Score = TotalScore;
                _context.Evaluations.Update(CurrentEvaluation);
            }

            await _context.SaveChangesAsync();

            if (!isNew)
            {
                var oldAnswers = _context.EvaluationAnswers.Where(a => a.EvaluationId == CurrentEvaluation.Id);
                _context.EvaluationAnswers.RemoveRange(oldAnswers);
                await _context.SaveChangesAsync();
            }

            foreach (var q in Questions)
            {
                _context.EvaluationAnswers.Add(new EvaluationAnswer
                {
                    EvaluationId = CurrentEvaluation.Id,
                    QuestionId = q.Id,
                    SelectedScore = q.SelectedScore
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // Audit
            _auditService.Log(
                _currentUsername,
                IsAdmin() ? "Admin" : "User",
                $"{(isNew ? "Added" : "Updated")} evaluation for supplier '{SelectedSupplier.Name}' - Score: {TotalScore} (Class {Classification})",
                "Evaluations"
            );

            IsEditing = true;
            CurrentEvaluation.ModifiedDate = DateTime.Now;
            ModifiedDateDisplay = CurrentEvaluation.ModifiedDate.ToString("dd-MM-yyyy HH:mm");
            if (isNew)
                CreatedDateDisplay = CurrentEvaluation.CreatedDate.ToString("dd-MM-yyyy HH:mm");

            StatusMessage = $"Evaluation saved successfully for {SelectedSupplier.Name}.";
            MessageBox.Show("Evaluation saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);

            await LoadEvaluationRecordsAsync();
        }
        catch (System.Exception ex)
        {
            _context.ChangeTracker.Clear();
            MessageBox.Show($"Error saving evaluation: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void UploadDocuments()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Multiselect = true,
            Filter = "PDF Files (*.pdf)|*.pdf|All Files (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var fileName in dialog.FileNames)
            {
                try
                {
                    var bytes = System.IO.File.ReadAllBytes(fileName);
                    var savedPath = _fileService.SaveFile(bytes, System.IO.Path.GetFileName(fileName), "EvaluationDocuments");
                    var doc = new EvaluationDocument
                    {
                        FileName = System.IO.Path.GetFileName(fileName),
                        FilePath = savedPath,
                        UploadedDate = DateTime.Now,
                        EvaluationId = CurrentEvaluation?.Id ?? 0
                    };
                    Documents.Add(doc);

                    StatusMessage = $"Uploaded: {doc.FileName}";
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show($"Error uploading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    [RelayCommand]
    private void RemoveDocument(EvaluationDocument doc)
    {
        if (doc == null) return;
        var result = MessageBox.Show($"Remove '{doc.FileName}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                _fileService.DeleteFile(doc.FilePath);
                Documents.Remove(doc);

                // If evaluation is already saved, delete from DB too
                if (CurrentEvaluation?.Id > 0)
                {
                    var toRemove = _context.EvaluationDocuments.FirstOrDefault(d => d.Id == doc.Id);
                    if (toRemove != null)
                    {
                        _context.EvaluationDocuments.Remove(toRemove);
                        _context.SaveChanges();
                    }
                }
                StatusMessage = $"Removed: {doc.FileName}";
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Error removing file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private void EditRecord(EvaluationRecord record)
    {
        if (record == null) return;
        var supplier = Suppliers.FirstOrDefault(s => s.Name == record.SupplierName);
        if (supplier != null)
        {
            SelectedSupplier = supplier;
        }
    }

    [RelayCommand]
    private async Task DeleteRecord(EvaluationRecord record)
    {
        if (record == null) return;
        var result = MessageBox.Show($"Delete evaluation for '{record.SupplierName}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            var eval = await _context.Evaluations
                .Include(e => e.Answers)
                .Include(e => e.Documents)
                .FirstOrDefaultAsync(e => e.Id == record.EvaluationId);
            if (eval != null)
            {
                // Delete associated files from disk
                foreach (var doc in eval.Documents)
                    _fileService.DeleteFile(doc.FilePath);

                _context.Evaluations.Remove(eval);
                await _context.SaveChangesAsync();

                _auditService.Log(
                    _currentUsername,
                    IsAdmin() ? "Admin" : "User",
                    $"Deleted evaluation for supplier '{record.SupplierName}'",
                    "Evaluations"
                );

                StatusMessage = $"Deleted evaluation for {record.SupplierName}.";
                await LoadEvaluationRecordsAsync();
                if (SelectedSupplier?.Name == record.SupplierName)
                    NewEvaluation();
            }
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Error deleting: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private bool IsAdmin()
    {
        var session = Application.Current.Properties["Session"] as dynamic;
        return session?.IsAdmin ?? false;
    }

    // Helper classes
    public class EvaluationRecord
    {
        public string SupplierName { get; set; } = string.Empty;
        public string EvaluationDate { get; set; } = string.Empty;
        public int Score { get; set; }
        public string Class { get; set; } = string.Empty;
        public string SelfDeclaration { get; set; } = string.Empty;
        public int EvaluationId { get; set; }
        public string Action { get; set; } = string.Empty;
    }
}

public class EvaluationQuestion : ObservableObject
{
    public int Id { get; }
    public string Category { get; }
    public string QuestionText { get; }
    public string[] OptionLabels { get; }
    public int[] OptionScores { get; }

    private int _selectedScore = -1; // -1 means not selected
    public int SelectedScore
    {
        get => _selectedScore;
        set
        {
            if (SetProperty(ref _selectedScore, value))
            {
                // Notify that answer changed so total can be recalculated
                // In the view model we'll subscribe to this event
            }
        }
    }

    public EvaluationQuestion(int id, string category, string questionText, string[] optionLabels, int[] optionScores)
    {
        Id = id;
        Category = category;
        QuestionText = questionText;
        OptionLabels = optionLabels;
        OptionScores = optionScores;
    }
}