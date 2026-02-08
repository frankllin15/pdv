using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PDV.Shared.DTOs;

namespace PDV.Desktop.ViewModels;

public partial class PaginationState : ObservableObject
{
    private readonly Func<Task> _loadPage;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private bool _hasNextPage;

    [ObservableProperty]
    private bool _hasPreviousPage;

    public PaginationState(Func<Task> loadPage)
    {
        _loadPage = loadPage;
    }

    public void Update<T>(PagedResult<T> result)
    {
        CurrentPage = result.Page;
        TotalPages = result.TotalPages;
        TotalCount = result.TotalCount;
        HasNextPage = result.HasNextPage;
        HasPreviousPage = result.HasPreviousPage;
    }

    public void Update(int totalCount, int page, int pageSize)
    {
        TotalCount = totalCount;
        CurrentPage = page;
        TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        HasPreviousPage = page > 1;
        HasNextPage = page < TotalPages;
    }

    public void Reset()
    {
        CurrentPage = 1;
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        if (!HasNextPage) return;
        CurrentPage++;
        await _loadPage();
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        if (!HasPreviousPage) return;
        CurrentPage--;
        await _loadPage();
    }
}
