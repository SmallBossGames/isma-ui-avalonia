using System.ComponentModel;
using Avalonia.Controls;
using Avalonia;
using ISMA.BlueprintEditor.ViewModels;

namespace ISMA.BlueprintEditor.Controls;

/// <summary>
/// Popover for editing a transaction's alias and predicate. The text boxes are
/// two-way synchronized with the <see cref="TransactionViewModel"/>. Ported
/// from the original Kotlin/JavaFX <c>EditArrowPopOver</c>.
/// </summary>
public partial class EditArrowPopOver : UserControl
{
    private TransactionViewModel? _transaction;

    /// <summary>Creates an empty popover (no transaction bound).</summary>
    public EditArrowPopOver()
    {
        InitializeComponent();

        AliasBox.PropertyChanged += OnBoxTextChanged;
        PredicateBox.PropertyChanged += OnBoxTextChanged;
    }

    /// <summary>Creates a popover bound to the given transaction.</summary>
    /// <param name="transaction">The transaction to edit.</param>
    public EditArrowPopOver(TransactionViewModel transaction)
        : this()
    {
        Transaction = transaction;
    }

    /// <summary>The transaction being edited (drives the text boxes).</summary>
    public TransactionViewModel? Transaction
    {
        get => _transaction;
        set
        {
            if (_transaction is { } old)
            {
                old.PropertyChanged -= OnTransactionPropertyChanged;
            }

            _transaction = value;

            if (value is { } tx)
            {
                tx.PropertyChanged += OnTransactionPropertyChanged;
                AliasBox.Text = tx.Alias;
                PredicateBox.Text = tx.Predicate;
            }
        }
    }

    private void OnTransactionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_transaction is not { } tx)
        {
            return;
        }

        switch (e.PropertyName)
        {
            case nameof(TransactionViewModel.Alias):
                AliasBox.Text = tx.Alias;
                break;
            case nameof(TransactionViewModel.Predicate):
                PredicateBox.Text = tx.Predicate;
                break;
        }
    }

    private void OnBoxTextChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property.Name != nameof(TextBox.Text) || _transaction is not { } tx)
        {
            return;
        }

        if (sender == AliasBox)
        {
            tx.Alias = AliasBox.Text ?? string.Empty;
        }
        else if (sender == PredicateBox)
        {
            tx.Predicate = PredicateBox.Text ?? string.Empty;
        }
    }
}
