using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Core.Entities;
using PDV.Core.Interfaces.Queries;
using PDV.Core.Interfaces.Repositories;
using PDV.Shared.DTOs;
using Res = PDV.Desktop.I18n.Resources;

namespace PDV.Desktop.ViewModels;

public partial class ProductsViewModel : ViewModelBase
{
    private readonly IProductQuery _productQuery;
    private readonly IUnitOfWork _unitOfWork;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _statusMessage = Res.Global_Lbl_Ready;

    [ObservableProperty]
    private bool _isError;

    [ObservableProperty]
    private bool _isEditing = false;

    [ObservableProperty]
    private bool _isNewProduct;

    [ObservableProperty]
    private ProductItemViewModel? _selectedProduct;

    [ObservableProperty]
    private ProductItemViewModel _editingProduct = new();

    public ObservableCollection<ProductItemViewModel> Products { get; } = new();

    public ProductsViewModel(IProductQuery productQuery, IUnitOfWork unitOfWork)
    {
        _productQuery = productQuery;
        _unitOfWork = unitOfWork;
    }

    [RelayCommand]
    private async Task LoadProductsAsync()
    {
        try
        {
            SetStatus(Res.Products_Msg_Loading, false);
            Products.Clear();

            var products = string.IsNullOrWhiteSpace(SearchText)
                ? await _productQuery.GetAllActiveAsync()
                : await _productQuery.SearchAsync(SearchText);

            foreach (var product in products)
            {
                Products.Add(MapToViewModel(product));
            }

            SetStatus(string.Format(Res.Products_Msg_FoundCount, Products.Count), false);
        }
        catch (Exception ex)
        {
            SetStatus(string.Format(Res.Products_Msg_LoadError, ex.Message), true);
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await LoadProductsAsync();
    }

    [RelayCommand]
    private void NewProduct()
    {
        EditingProduct = new ProductItemViewModel
        {
            UnitOfMeasure = "UN",
            IsActive = true
        };
        IsNewProduct = true;
        IsEditing = true;
        SetStatus(Res.Products_Msg_CreatingNew, false);
    }

    [RelayCommand]
    private void EditProduct(ProductItemViewModel? product)
    {
        if (product == null) return;

        EditingProduct = new ProductItemViewModel
        {
            Id = product.Id,
            Barcode = product.Barcode,
            Description = product.Description,
            ShortDescription = product.ShortDescription,
            UnitPrice = product.UnitPrice,
            UnitOfMeasure = product.UnitOfMeasure,
            StockQuantity = product.StockQuantity,
            TaxCode = product.TaxCode,
            TaxRate = product.TaxRate,
            IsActive = product.IsActive
        };
        IsNewProduct = false;
        IsEditing = true;
        SetStatus(string.Format(Res.Products_Msg_Editing, product.Description), false);
    }

    [RelayCommand]
    private async Task SaveProductAsync()
    {
        if (string.IsNullOrWhiteSpace(EditingProduct.Barcode))
        {
            SetStatus(Res.Products_Msg_BarcodeRequired, true);
            return;
        }

        if (string.IsNullOrWhiteSpace(EditingProduct.Description))
        {
            SetStatus(Res.Products_Msg_DescriptionRequired, true);
            return;
        }

        if (EditingProduct.UnitPrice < 0)
        {
            SetStatus(Res.Products_Msg_PriceNegative, true);
            return;
        }

        try
        {
            if (IsNewProduct)
            {
                await CreateProductAsync();
            }
            else
            {
                await UpdateProductAsync();
            }

            IsEditing = false;
            await LoadProductsAsync();
        }
        catch (Exception ex)
        {
            SetStatus(string.Format(Res.Products_Msg_SaveError, ex.Message), true);
        }
    }

    private async Task CreateProductAsync()
    {
        // Check if barcode already exists
        var existing = await _productQuery.GetByBarcodeAsync(EditingProduct.Barcode);
        if (existing != null)
        {
            SetStatus(string.Format(Res.Products_Msg_BarcodeExists, EditingProduct.Barcode), true);
            return;
        }

        var product = new Product(
            barcode: EditingProduct.Barcode,
            description: EditingProduct.Description,
            unitPrice: EditingProduct.UnitPrice,
            shortDescription: string.IsNullOrWhiteSpace(EditingProduct.ShortDescription)
                ? EditingProduct.Description
                : EditingProduct.ShortDescription,
            unitOfMeasure: EditingProduct.UnitOfMeasure,
            stockQuantity: EditingProduct.StockQuantity,
            taxCode: EditingProduct.TaxCode,
            taxRate: EditingProduct.TaxRate);

        await _unitOfWork.Products.AddAsync(product);
        await _unitOfWork.SaveChangesAsync();

        SetStatus(string.Format(Res.Products_Msg_Created, product.Description), false);
    }

    private async Task UpdateProductAsync()
    {
        var product = await _unitOfWork.Products.GetByIdAsync(EditingProduct.Id);
        if (product == null)
        {
            SetStatus(Res.Products_Msg_ProductNotFound, true);
            return;
        }

        // Check if barcode is being changed to one that already exists
        if (product.Barcode != EditingProduct.Barcode)
        {
            var existing = await _productQuery.GetByBarcodeAsync(EditingProduct.Barcode);
            if (existing != null && existing.Id != EditingProduct.Id)
            {
                SetStatus(string.Format(Res.Products_Msg_BarcodeExists, EditingProduct.Barcode), true);
                return;
            }
        }

        product.SetBarcode(EditingProduct.Barcode);
        product.SetDescription(EditingProduct.Description);
        product.SetUnitPrice(EditingProduct.UnitPrice);

        // Update stock
        var stockDiff = EditingProduct.StockQuantity - product.StockQuantity;
        if (stockDiff != 0)
        {
            product.UpdateStock(stockDiff);
        }

        if (EditingProduct.IsActive)
        {
            product.Activate();
        }
        else
        {
            product.Deactivate();
        }

        await _unitOfWork.SaveChangesAsync();

        SetStatus(string.Format(Res.Products_Msg_Updated, product.Description), false);
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        EditingProduct = new();
        SetStatus(Res.Products_Msg_EditCancelled, false);
    }

    [RelayCommand]
    private async Task DeleteProductAsync(ProductItemViewModel? product)
    {
        if (product == null) return;

        try
        {
            var entity = await _unitOfWork.Products.GetByIdAsync(product.Id);
            if (entity != null)
            {
                entity.Deactivate();
                await _unitOfWork.SaveChangesAsync();
                SetStatus(string.Format(Res.Products_Msg_Deactivated, product.Description), false);
                await LoadProductsAsync();
            }
        }
        catch (Exception ex)
        {
            SetStatus(string.Format(Res.Products_Msg_DeleteError, ex.Message), true);
        }
    }

    [RelayCommand]
    private async Task AdjustStockAsync(ProductItemViewModel? product)
    {
        if (product == null || product.StockAdjustment == 0) return;

        try
        {
            var entity = await _unitOfWork.Products.GetByIdAsync(product.Id);
            if (entity != null)
            {
                entity.UpdateStock(product.StockAdjustment);
                await _unitOfWork.SaveChangesAsync();

                product.StockQuantity = entity.StockQuantity;
                product.StockAdjustment = 0;

                SetStatus(string.Format(Res.Products_Msg_StockUpdated, product.Description, entity.StockQuantity), false);
            }
        }
        catch (Exception ex)
        {
            SetStatus(string.Format(Res.Products_Msg_StockError, ex.Message), true);
        }
    }

    private void SetStatus(string message, bool isError)
    {
        StatusMessage = message;
        IsError = isError;
    }

    private static ProductItemViewModel MapToViewModel(ProductDto dto) => new()
    {
        Id = dto.Id,
        Barcode = dto.Barcode,
        Description = dto.Description,
        ShortDescription = dto.ShortDescription,
        UnitPrice = dto.UnitPrice,
        UnitOfMeasure = dto.UnitOfMeasure,
        StockQuantity = dto.StockQuantity,
        TaxCode = dto.TaxCode,
        TaxRate = dto.TaxRate,
        IsActive = dto.IsActive
    };
}

public partial class ProductItemViewModel : ObservableObject
{
    public Guid Id { get; set; }

    [ObservableProperty]
    private string _barcode = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _shortDescription = string.Empty;

    [ObservableProperty]
    private decimal _unitPrice;

    [ObservableProperty]
    private string _unitOfMeasure = "UN";

    [ObservableProperty]
    private decimal _stockQuantity;

    [ObservableProperty]
    private string? _taxCode;

    [ObservableProperty]
    private decimal _taxRate;

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private decimal _stockAdjustment;
}
