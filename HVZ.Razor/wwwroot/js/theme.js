// Apply themes to the document
getTheme()

/**
 * Get the user's saved theme preference or use their system theme
 */
function getTheme() {
    let theme = getThemePreference();
    if (theme !== null) {
        applyTheme(theme);
        return;
    }

    const darkThemeMq = window.matchMedia("(prefers-color-scheme: dark)");
    if (darkThemeMq.matches)
        theme = "dark";
    else
        theme = "light";

    saveThemePreference(theme);
    applyTheme(theme);
}

/**
 * Apply a theme to the document
 * @param {string} theme Theme preference (light/dark)
 */
function applyTheme(theme) {
    document.getElementsByTagName("html")[0].setAttribute("data-bs-theme", theme);
    applyThemedFavicon(theme)
}

/**
 * Save the user's theme preference to local storage
 * @param {string} preference Theme preference (light/dark)*/
function saveThemePreference(preference) {
    localStorage.setItem("theme", preference);
}

/**
 * Get the user's theme preference from local storage
 * @returns string 
 */
function getThemePreference() {
    return localStorage.getItem("theme");
}

/**
 * Apply the themed favicon to the webpage
 * @param {string} theme Theme preference (light/dark)
 */
function applyThemedFavicon(theme) {
    const basePath = `/favicon/${theme}/`;
    const touchIco = document.getElementById("apple-touch-icon");
    const icon32 = document.getElementById("icon-32");
    const icon16 = document.getElementById("icon-16");
    const manifest = document.getElementById("manifest");

    touchIco.setAttribute("herf", basePath + "apple-touch-icon.png");
    icon32.setAttribute("href", basePath + "favicon-32x32.png");
    icon16.setAttribute("href", basePath + "favicon-16x16.png");
    manifest.setAttribute("href", basePath + "site.webmanifest");
}