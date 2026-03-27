(function () {
    var profileToggle = document.getElementById("fbnProfileToggle");
    var profileDropdown = document.getElementById("fbnProfileDropdown");

    if (!profileToggle || !profileDropdown) {
        return;
    }

    function setDropdownOpen(isOpen) {
        profileDropdown.classList.toggle("show", Boolean(isOpen));
        profileToggle.setAttribute("aria-expanded", isOpen ? "true" : "false");
    }

    profileToggle.addEventListener("click", function (event) {
        event.stopPropagation();
        var shouldOpen = !profileDropdown.classList.contains("show");
        setDropdownOpen(shouldOpen);
    });

    document.addEventListener("click", function (event) {
        if (!profileDropdown.contains(event.target) && !profileToggle.contains(event.target)) {
            setDropdownOpen(false);
        }
    });

    document.addEventListener("keydown", function (event) {
        if (event.key === "Escape") {
            setDropdownOpen(false);
        }
    });
})();
