using Microsoft.JSInterop;

namespace Website.Util
{
    public interface ILanguageService
    {
        Task SetCultureAsync(string culture);
    }

    public class LanguageService : ILanguageService
    {
        private readonly IJSRuntime _jsRuntime;

        public LanguageService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task SetCultureAsync(string culture)
        {
            await _jsRuntime.InvokeVoidAsync("setCulture", culture);
        }
    }
}
