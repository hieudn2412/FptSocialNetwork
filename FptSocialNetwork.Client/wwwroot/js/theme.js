(function () {
    function applyDarkTheme() {
        if (document.documentElement) {
            document.documentElement.setAttribute("data-theme", "dark");
        }

        if (document.body) {
            document.body.setAttribute("data-theme", "dark");
        }
    }

    function initTheme() {
        applyDarkTheme();
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initTheme, { once: true });
    } else {
        initTheme();
    }

    window.fptTheme = {
        current: function () { return "dark"; },
        set: applyDarkTheme
    };
})();
