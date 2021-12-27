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
            this.detectUserPreferenceIfNotSet();
            this.applyTheme();

            document.addEventListener("DOMContentLoaded", () => {
                const darkmodeButton = document.getElementById("btnToggleDarkmode");

                if (darkmodeButton) {
                    darkmodeButton.addEventListener("click", () => this.toggleDarkmode());
                }
            });
        }

        private detectUserPreferenceIfNotSet(): void {
            let preferDarkModeQuery = window.matchMedia("(prefers-color-scheme: dark)");
            if (!this.preferences.isSet()) {
                this.preferences.save(preferDarkModeQuery.matches);
            }
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
        private readonly key = "darkmode-enabled";

        public save(enabled: boolean) {
            localStorage.setItem(this.key, enabled ? this.trueLiteral : this.falseLiteral);
        }

        public isSet(): boolean {
            var fromStorage = localStorage.getItem(this.key);

            return fromStorage !== null;
        }

        public get(): boolean {
            var enabled = localStorage.getItem(this.key);

            return enabled && enabled === this.trueLiteral;
        }
    }

    // Get this show on the road.
    init();
}