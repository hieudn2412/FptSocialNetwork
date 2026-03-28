(function () {
    const root = document.querySelector("[data-social-realtime]");
    const pageType = root?.getAttribute("data-social-page") || "index";
    const postFeed = document.querySelector("[data-post-feed]");
    const toastHost = document.getElementById("socialToastHost") || document.body;
    const editModal = document.getElementById("edit-post-modal");
    const editForm = document.getElementById("edit-post-form");
    const editTextarea = document.getElementById("edit-post-textarea");
    const editPostIdInput = document.getElementById("edit-post-id");
    const editPostSourceInput = document.getElementById("edit-post-source");

    const showToast = (message) => {
        if (!message || !toastHost) {
            return;
        }

        const toast = document.createElement("div");
        toast.className = "social-toast";
        toast.textContent = message;
        toastHost.appendChild(toast);

        window.setTimeout(() => {
            toast.classList.add("hide");
            window.setTimeout(() => toast.remove(), 300);
        }, 2200);
    };

    const parseJson = async (response) => {
        const contentType = response.headers.get("content-type") || "";
        if (!contentType.includes("application/json")) {
            return null;
        }

        return response.json();
    };

    const findArticle = (element) => element?.closest("[data-post-id]");

    const ensureCommentList = (article) => {
        if (!article) {
            return null;
        }

        let list = article.querySelector("[data-comment-list]");
        if (!list) {
            list = document.createElement("div");
            list.className = "comment-list";
            list.setAttribute("data-comment-list", "");
            const form = article.querySelector(".comment-form");
            if (form) {
                article.insertBefore(list, form);
            } else {
                article.appendChild(list);
            }
        }

        return list;
    };

    const syncEmptyState = () => {
        if (!postFeed) {
            return;
        }

        const hasPost = postFeed.querySelector("[data-post-id]");
        const existingEmpty = postFeed.querySelector("[data-empty-posts]");
        if (hasPost) {
            existingEmpty?.remove();
            return;
        }

        if (existingEmpty) {
            return;
        }

        const empty = document.createElement("div");
        empty.setAttribute("data-empty-posts", "");
        if (pageType === "profile") {
            empty.className = "card center";
            empty.style.marginTop = "12px";
            empty.textContent = "Bạn chưa có bài viết nào.";
        } else {
            empty.className = "card empty-state";
            empty.textContent = "Chưa có bài viết nào trong bảng tin.";
        }

        postFeed.appendChild(empty);
    };

    const updateLikeState = (article, button, payload) => {
        if (!article || !payload) {
            return;
        }

        const likeCount = article.querySelector("[data-like-count]");
        if (likeCount) {
            likeCount.textContent = (payload.likeCount ?? 0) + " lượt thích";
        }

        button?.classList.toggle("active", Boolean(payload.isLiked));
        const icon = button?.querySelector("i");
        if (icon) {
            icon.classList.toggle("fa-solid", Boolean(payload.isLiked));
            icon.classList.toggle("fa-regular", !payload.isLiked);
        }

        const label = button?.querySelector("[data-like-label]");
        if (label) {
            label.textContent = payload.isLiked ? "Bỏ thích" : "Thích";
        }
    };

    const appendComment = (article, payload) => {
        if (!article || !payload) {
            return;
        }

        const list = ensureCommentList(article);
        if (!list) {
            return;
        }

        const commentId = payload.id;
        if (commentId && list.querySelector('[data-comment-id="' + commentId + '"]')) {
            return;
        }

        list.querySelector("[data-comment-empty]")?.remove();

        const authorName = payload.authorName || "Người dùng";
        const item = document.createElement("div");
        item.className = "comment-item";
        if (commentId) {
            item.setAttribute("data-comment-id", String(commentId));
        }

        item.innerHTML = '<div class="comment-meta"><a href="/Home/Profile/' + encodeURIComponent(payload.userId) + '"><strong>'
            + authorName.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;").replace(/'/g, "&#39;")
            + "</strong></a><span>Vừa xong</span></div><div class=\"comment-content\">"
            + String(payload.content || "").replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;").replace(/'/g, "&#39;")
            + "</div>";
        list.appendChild(item);

        while (list.children.length > 5) {
            list.removeChild(list.firstElementChild);
        }

        const countNode = article.querySelector("[data-comment-count]");
        if (countNode) {
            const currentCount = parseInt(countNode.textContent, 10) || 0;
            countNode.textContent = (currentCount + 1) + " bình luận";
        }
    };

    const updatePostContent = (article, payload) => {
        if (!article || !payload) {
            return;
        }

        const nextContent = (payload.content || "").trim();
        let contentNode = article.querySelector(".post-content");
        const sharedCard = article.querySelector(".shared-card");
        const mediaNode = article.querySelector(".post-image");

        if (!nextContent) {
            contentNode?.remove();
            return;
        }

        if (!contentNode) {
            contentNode = document.createElement("div");
            contentNode.className = "post-content";
            const insertBeforeTarget = sharedCard || mediaNode || article.querySelector(".post-stats");
            if (insertBeforeTarget) {
                article.insertBefore(contentNode, insertBeforeTarget);
            } else {
                article.appendChild(contentNode);
            }
        }

        contentNode.textContent = nextContent;
    };

    const removePost = (article) => {
        if (!article) {
            return;
        }

        article.remove();
        syncEmptyState();
    };

    const updateFollowState = (form, payload) => {
        if (!form || !payload) {
            return;
        }

        const targetUserId = form.querySelector('input[name="targetUserId"]')?.value;
        if (!targetUserId) {
            return;
        }

        document.querySelectorAll('form[action$="/ToggleFollow"]').forEach((candidate) => {
            const candidateUserId = candidate.querySelector('input[name="targetUserId"]')?.value;
            if (candidateUserId !== targetUserId) {
                return;
            }

            candidate.querySelectorAll("button").forEach((button) => {
                button.classList.toggle("following", Boolean(payload.isFollowing));
                if (button.classList.contains("follow-primary") || button.classList.contains("follow-secondary")) {
                    button.classList.toggle("follow-primary", !payload.isFollowing);
                    button.classList.toggle("follow-secondary", Boolean(payload.isFollowing));
                }

                button.textContent = payload.isFollowing ? "Bỏ theo dõi" : "Theo dõi";
            });
        });
    };

    const closeMenus = () => {
        document.querySelectorAll(".post-menu.open").forEach((menu) => menu.classList.remove("open"));
    };

    const openEditModal = (article) => {
        if (!article || !editModal || !editForm || !editTextarea || !editPostIdInput) {
            return;
        }

        editPostIdInput.value = article.getAttribute("data-post-id") || "";
        if (editPostSourceInput) {
            editPostSourceInput.value = pageType;
        }

        editTextarea.value = article.querySelector(".post-content")?.textContent?.trim() || "";
        editModal.hidden = false;
        document.body.style.overflow = "hidden";
        closeMenus();
        window.setTimeout(() => editTextarea.focus(), 0);
    };

    const closeEditModal = () => {
        if (!editModal) {
            return;
        }

        editModal.hidden = true;
        document.body.style.overflow = "";
    };

    const handleSubmit = async (form, event) => {
        if (!form) {
            return;
        }

        event.preventDefault();

        const action = form.action || "";
        const article = findArticle(form);
        const submitButton = form.querySelector('button[type="submit"]');
        if (action.endsWith("/DeletePost") && !window.confirm("Bạn có chắc muốn xóa bài viết này không?")) {
            return;
        }

        submitButton?.setAttribute("disabled", "disabled");

        try {
            const response = await fetch(action, {
                method: "POST",
                body: new FormData(form),
                headers: { "X-Requested-With": "XMLHttpRequest" },
                credentials: "include"
            });

            if (response.redirected) {
                window.location.href = response.url;
                return;
            }

            const payload = await parseJson(response);
            if (!response.ok) {
                const text = await response.text();
                throw new Error((payload && payload.error) || text || "Thao tác thất bại.");
            }

            if (action.endsWith("/ToggleReaction")) {
                updateLikeState(article, submitButton, payload);
                return;
            }

            if (action.endsWith("/AddComment")) {
                appendComment(article, payload);
                const input = form.querySelector('input[name="content"]');
                if (input) {
                    input.value = "";
                }
                return;
            }

            if (action.endsWith("/SharePost")) {
                showToast("Đã chia sẻ bài viết.");
                return;
            }

            if (action.endsWith("/ToggleFollow")) {
                updateFollowState(form, payload);
                return;
            }

            if (action.endsWith("/EditPost")) {
                const editedArticle = document.querySelector('[data-post-id="' + payload.id + '"]');
                updatePostContent(editedArticle, payload);
                closeEditModal();
                showToast("Đã cập nhật bài viết.");
                return;
            }

            if (action.endsWith("/DeletePost")) {
                removePost(article || document.querySelector('[data-post-id="' + payload.postId + '"]'));
                closeMenus();
                showToast("Đã xóa bài viết.");
            }
        } catch (error) {
            showToast(error.message || "Thao tác thất bại.");
        } finally {
            submitButton?.removeAttribute("disabled");
        }
    };

    document.addEventListener("submit", (event) => {
        const form = event.target.closest(".post-action-form, .comment-form, .post-delete-form, form[action$=\"/ToggleFollow\"], #edit-post-form");
        if (!form) {
            return;
        }

        handleSubmit(form, event);
    });

    document.addEventListener("click", (event) => {
        const focusButton = event.target.closest("[data-focus-comment]");
        if (focusButton) {
            const targetId = focusButton.getAttribute("data-focus-comment");
            if (targetId) {
                document.getElementById(targetId)?.focus();
            }
            return;
        }

        const menuToggle = event.target.closest(".post-menu-toggle");
        if (menuToggle) {
            event.stopPropagation();
            const menu = menuToggle.parentElement?.querySelector(".post-menu");
            const willOpen = !menu?.classList.contains("open");
            closeMenus();
            menu?.classList.toggle("open", Boolean(willOpen));
            return;
        }

        const editButton = event.target.closest("[data-edit-post]");
        if (editButton) {
            openEditModal(findArticle(editButton));
            return;
        }

        const closeEditButton = event.target.closest("[data-close-edit-post-modal]");
        if (closeEditButton) {
            closeEditModal();
            return;
        }

        if (editModal && event.target === editModal) {
            closeEditModal();
            return;
        }

        closeMenus();
    });

    document.addEventListener("keydown", (event) => {
        if (event.key !== "Escape") {
            return;
        }

        closeMenus();
        closeEditModal();
    });
})();
