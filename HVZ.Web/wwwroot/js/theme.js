
/// Get the user theme preference from local storage and apply it to the page

getTheme();

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

function applyTheme(theme) {
    console.log(theme)
    document.getElementsByTagName("html")[0].setAttribute("data-bs-theme", theme)
    applyThemedFavicon(theme);
}

function saveThemePreference(preference) {
    localStorage.setItem("theme", preference);
}

function getThemePreference() {
    return localStorage.getItem("theme");
}

function applyThemedFavicon(theme) {
    // Append the proper links to the head
    let basePath = `/favicon/${theme}/`
    let touchIco = document.getElementById("apple-touch-icon");
    let icon32 = document.getElementById("icon-32");
    let icon16 = document.getElementById("icon-16");
    let manifest = document.getElementById("manifest");

    touchIco.setAttribute("href", basePath + "apple-touch-icon.png");
    icon32.setAttribute("href", basePath + "favicon-32x32.png");
    icon16.setAttribute("href", basePath + "favicon-16x16.png");
    manifest.setAttribute("href", basePath + "site.webmanifest");
}