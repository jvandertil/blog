function toggleStylesheet(dark) {
    var stylesheetName;

    if (dark) {
        stylesheetName = "vs.min.css";
    } else {
        stylesheetName = "vs2015.min.css";
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

function isDarkmodeEnabled() {
    var body = document.getElementsByTagName("body")[0];
    var darkModeEnabled = body.classList.contains('dark');

    return darkModeEnabled;
}

function saveDarkmodePreference(enabled) {
    localStorage.setItem("darkmode-enabled", enabled.toString());
}

function applyDarkmode() {
    var enabled = localStorage.getItem("darkmode-enabled");
    var body = document.getElementsByTagName("body")[0];

    if (enabled == null) {
        enabled = isDarkmodeEnabled().toString();
    }

    if (enabled === "true") {
        body.classList.remove('light');
        body.classList.add('dark');
        toggleStylesheet(true);
    } else {
        body.classList.replace('dark', 'light');
        toggleStylesheet(false);
    }
}

function toggleDarkmode() {
    saveDarkmodePreference(!isDarkmodeEnabled());

    applyDarkmode();
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

applyDarkmode();
