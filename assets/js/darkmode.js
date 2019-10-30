function ApplyCodeHighlightingStyle(theme) {
    var stylesheetName;

    switch (theme) {
        case "dark": {
            stylesheetName = "vs.min.css";
            break;
        }
        case "light": {
            stylesheetName = "vs2015.min.css";
            break;
        }
    }

    var styleSheets = document.styleSheets;

    for (var i = 0; i < styleSheets.length; ++i) {
        var ss = styleSheets[i];

        if (ss.href) {
            if (ss.href.indexOf("highlight") >= 0) {
                if (ss.href.endsWith(stylesheetName)) {
                    ss.disabled = true;
                } else {
                    ss.disabled = false;
                }
            }
        }
    }
}

function getTheme() {
    var body = document.getElementsByTagName("body")[0];
    var theme = body.getAttribute("data-theme");

    if (!theme) {
        theme = "light";
    }

    return theme;
}

function setTheme(theme) {
    var body = document.getElementsByTagName("body")[0];
    body.setAttribute("data-theme", theme);

    ApplyCodeHighlightingStyle(theme);
}

function isDarkmodeEnabled() {
    var darkModeEnabled = getTheme() === "dark";

    return darkModeEnabled;
}

function saveDarkmodePreference(enabled) {
    localStorage.setItem("darkmode-enabled", enabled.toString());
}

function applyTheme() {
    var enabled = localStorage.getItem("darkmode-enabled");

    if (enabled == null) {
        enabled = isDarkmodeEnabled().toString();
    }

    if (enabled === "true") {
        setTheme("dark");
    } else {
        setTheme("light");
    }
}

function toggleDarkmode() {
    saveDarkmodePreference(!isDarkmodeEnabled());

    applyTheme();
}

function toggleDarkmodeKeyPress(event) {
    var KEY_ENTER = 13;
    var KEY_SPACE = 32;

    switch (event.which) {
        case KEY_ENTER:
        case KEY_SPACE: {
            toggleDarkmode();
        }
    }
}

function initializeTheme() {
    applyTheme();
}