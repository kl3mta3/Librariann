using System.Collections.Generic;
using System.Threading.Tasks;
using Librariann.Models.DTOs;

namespace Librariann.API.Services;

public interface ILocalizationService
{
    Task<string> GetAsync(string locale, string key, params object[] args);
    /// <summary>
    /// Returns a translated string for the currently authenticated user (Via <see cref="Librariann.API.Store.IUserContext"/>).
    /// Falling back to English or the key
    /// </summary>
    /// <param name="key"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    Task<string> TranslateAsync(string key, params object[] args);
    /// <summary>
    /// Returns a translated string for a given user's locale, falling back to english or the key if missing
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="key"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    Task<string> TranslateAsync(int userId, string key, params object[] args);
    IEnumerable<LibrariannLocale> GetLocales();
}
