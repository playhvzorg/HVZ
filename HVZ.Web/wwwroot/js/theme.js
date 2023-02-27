
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
}

function saveThemePreference(preference) {
    localStorage.setItem("theme", preference);
}

function getThemePreference() {
    return localStorage.getItem("theme");
}