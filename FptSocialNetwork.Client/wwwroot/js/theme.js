(function () {
    function applyLightTheme() {
        if (document.documentElement) {
            document.documentElement.setAttribute("data-theme", "light");
        }

        if (document.body) {
            document.body.setAttribute("data-theme", "light");
        }
    }

    function initTheme() {
        applyLightTheme();
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initTheme, { once: true });
    } else {
        initTheme();
    }

    window.fptTheme = {
        current: function () { return "light"; },
        set: applyLightTheme
    };
})();
