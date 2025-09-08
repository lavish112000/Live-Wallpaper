using System.Threading.Tasks;

namespace Lively.Common.Services
{
    public interface IWindowService
    {
        bool IsGridOverlayVisible { get; }
        void ShowLogWindow();
        void ShowGridOverlay(bool isVisible);
        Task<bool> ShowWallpaperDialogWindowAsync(object wallpaper);
    }
}