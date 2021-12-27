namespace Darkmode {
    function init() {
        new ThemeSwitcher().init();
    }

    export class ThemeSwitcher {
        private readonly preferences: PreferenceStore;

        constructor() {
            this.preferences = new PreferenceStore();
        }

        public init(): void {
            this.applyTheme();

            document.addEventListener("DOMContentLoaded", () => {
                const darkmodeButton = document.getElementById("btnToggleDarkmode");

                if (darkmodeButton) {
                    darkmodeButton.addEventListener("click", () => this.toggleDarkmode());
                }
            });
        }

        private toggleDarkmode(): void {
            const darkmodeEnabled = this.preferences.get();

            this.preferences.save(!darkmodeEnabled);
            this.applyTheme();
        }

        private applyTheme(): void {
            const darkmodeEnabled = this.preferences.get();

            this.enableTheme(darkmodeEnabled ? "dark" : "light");
        }

        private enableTheme(theme: string): void {
            const body = document.getElementsByTagName("body")[0];
            body.setAttribute("data-theme", theme);

            this.toggleThemeStylesheets(theme);
        }

        private toggleThemeStylesheets(theme) {
            var styleSheets = document.styleSheets;

            for (let i = 0; i < styleSheets.length; ++i) {
                const ss = styleSheets[i];

                if (ss.href) {
                    const node = ss.ownerNode as Element;

                    if (node) {
                        var themeTag = node.getAttribute("data-theme")

                        if (themeTag) {
                            if (themeTag === theme) {
                                ss.disabled = false;
                            } else {
                                ss.disabled = true;
                            }
                        }
                    }
                }
            }
        }
    }

    class PreferenceStore {
        private readonly trueLiteral = "true";
        private readonly falseLiteral = "false";

        public save(enabled: boolean) {
            localStorage.setItem("darkmode-enabled", enabled ? this.trueLiteral : this.falseLiteral);
        }

        public get(): boolean {
            var enabled = localStorage.getItem("darkmode-enabled");

            return enabled && enabled === this.trueLiteral;
        }
    }

    // Get this show on the road.
    init();
}