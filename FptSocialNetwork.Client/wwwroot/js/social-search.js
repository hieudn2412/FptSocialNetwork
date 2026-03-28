(function () {
    const searchRoots = Array.from(document.querySelectorAll("[data-user-search]"));
    if (!searchRoots.length) {
        return;
    }

    const escapeHtml = (value) => String(value ?? "")
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#39;");

    const profileUrl = (userId) => "/Home/Profile/" + encodeURIComponent(userId);

    searchRoots.forEach((root) => {
        const input = root.querySelector("[data-user-search-input]");
        const results = root.querySelector("[data-user-search-results]");
        if (!input || !results) {
            return;
        }

        let debounceId = 0;
        let currentItems = [];

        const closeResults = () => {
            currentItems = [];
            results.innerHTML = "";
            results.hidden = true;
        };

        const renderStatus = (message) => {
            results.innerHTML = '<div class="social-search-status">' + escapeHtml(message) + "</div>";
            results.hidden = false;
        };

        const renderItems = (items) => {
            currentItems = items;
            if (!items.length) {
                renderStatus("Không tìm thấy người dùng phù hợp.");
                return;
            }

            results.innerHTML = items.map((item, index) => {
                const name = item.fullName || "Người dùng";
                const initial = name.substring(0, 1).toUpperCase();
                const avatar = item.avatarUrl
                    ? '<img src="' + encodeURI(item.avatarUrl) + '" alt="avatar" />'
                    : '<span>' + escapeHtml(initial) + "</span>";

                return '<a class="social-search-link' + (index === 0 ? " active" : "") + '" href="' + profileUrl(item.userId) + '">'
                    + '<span class="social-search-avatar">' + avatar + "</span>"
                    + '<span class="social-search-copy"><strong>' + escapeHtml(name) + "</strong><span>" + escapeHtml(item.email || "") + "</span></span>"
                    + "</a>";
            }).join("");
            results.hidden = false;
        };

        const searchUsers = () => {
            const query = input.value.trim();
            if (query.length < 2) {
                closeResults();
                return;
            }

            renderStatus("Đang tìm kiếm...");
            fetch("/Home/SearchUsersJson?q=" + encodeURIComponent(query), {
                credentials: "include",
                headers: { "X-Requested-With": "XMLHttpRequest" }
            })
                .then(async (response) => {
                    if (response.status === 401) {
                        window.location.href = "/Account/Login";
                        return [];
                    }

                    if (!response.ok) {
                        let error = "Không thể tìm kiếm người dùng.";
                        try {
                            const payload = await response.json();
                            error = payload.error || error;
                        } catch {
                            // Ignore non-JSON errors and use the default message.
                        }

                        throw new Error(error);
                    }

                    return response.json();
                })
                .then((items) => {
                    if (!Array.isArray(items)) {
                        renderStatus("Không tìm thấy người dùng phù hợp.");
                        return;
                    }

                    renderItems(items);
                })
                .catch((error) => {
                    renderStatus(error.message || "Không thể tìm kiếm người dùng.");
                });
        };

        input.setAttribute("autocomplete", "off");

        input.addEventListener("input", () => {
            window.clearTimeout(debounceId);
            debounceId = window.setTimeout(searchUsers, 240);
        });

        input.addEventListener("focus", () => {
            if (input.value.trim().length >= 2 && currentItems.length > 0) {
                results.hidden = false;
            }
        });

        input.addEventListener("keydown", (event) => {
            if (event.key === "Escape") {
                closeResults();
                return;
            }

            if (event.key === "Enter") {
                const firstMatch = currentItems[0];
                if (!firstMatch) {
                    return;
                }

                event.preventDefault();
                window.location.href = profileUrl(firstMatch.userId);
            }
        });

        document.addEventListener("click", (event) => {
            if (root.contains(event.target)) {
                return;
            }

            closeResults();
        });
    });
})();
