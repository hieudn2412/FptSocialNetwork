(function () {
    const root = document.querySelector("[data-social-realtime]");
    if (!root || typeof signalR === "undefined") {
        console.warn("[social-realtime] root or signalR missing");
        return;
    }

    const hubUrl = root.getAttribute("data-social-hub-url");
    const token = root.getAttribute("data-realtime-token");
    const pageType = root.getAttribute("data-social-page") || "index";
    const currentUserId = Number.parseInt(root.getAttribute("data-current-user-id") || "0", 10) || 0;
    const viewedUserId = Number.parseInt(root.getAttribute("data-viewed-user-id") || "0", 10) || 0;
    if (!hubUrl || !token) {
        console.warn("[social-realtime] hub url or token missing", { hubUrlPresent: !!hubUrl, tokenPresent: !!token });
        return;
    }

    const bellButton = document.getElementById("socialNotificationToggle");
    const bellDropdown = document.getElementById("socialNotificationDropdown");
    const bellList = document.getElementById("socialNotificationList");
    const bellEmpty = document.getElementById("socialNotificationEmpty");
    const bellBadge = document.getElementById("socialNotificationBadge");
    const toastHost = document.getElementById("socialToastHost");

    const setUnreadCount = (count) => {
        if (!bellBadge) {
            return;
        }

        bellBadge.hidden = count <= 0;
        bellBadge.textContent = String(count);
    };

    let unreadCount = 0;
    setUnreadCount(unreadCount);
    const antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    const postFeed = document.querySelector("[data-post-feed]");

    const escapeHtml = (value) => String(value ?? "")
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#39;");

    const formatTimeAgo = (value) => {
        const date = new Date(value);
        if (Number.isNaN(date.getTime())) {
            return "Vừa xong";
        }

        const diffMinutes = Math.floor((Date.now() - date.getTime()) / 60000);
        if (diffMinutes < 1) return "Vừa xong";
        if (diffMinutes < 60) return diffMinutes + " phút";
        const diffHours = Math.floor(diffMinutes / 60);
        if (diffHours < 24) return diffHours + " giờ";
        const diffDays = Math.floor(diffHours / 24);
        if (diffDays < 7) return diffDays + " ngày";
        return date.toLocaleString("vi-VN");
    };

    const buildProfileUrl = (userId) => "/Home/Profile/" + encodeURIComponent(userId);
    const canManagePost = (post) => currentUserId > 0 && Number(post?.userId) === currentUserId;

    const buildAvatarMarkup = (avatarUrl, name, extraClass) => {
        const initial = (name || "Người dùng").trim().substring(0, 1).toUpperCase() || "N";
        const className = extraClass ? ' class="' + extraClass + '"' : "";
        if (avatarUrl) {
            return '<img' + className + ' src="' + encodeURI(avatarUrl) + '" alt="avatar" />';
        }

        return "<span>" + escapeHtml(initial) + "</span>";
    };

    const canRenderPostOnCurrentPage = (post) => {
        if (!post) {
            return false;
        }

        if (pageType === "profile") {
            return Number(post.userId) === viewedUserId;
        }

        return true;
    };

    const buildFollowMarkup = (post) => {
        if (!post || !currentUserId || Number(post.userId) === currentUserId) {
            return "";
        }

        return '<form class="follow-form" action="/Home/ToggleFollow" method="post">'
            + '<input name="__RequestVerificationToken" type="hidden" value="' + escapeHtml(antiForgeryToken) + '" />'
            + '<input type="hidden" name="targetUserId" value="' + escapeHtml(post.userId) + '" />'
            + '<input type="hidden" name="source" value="' + escapeHtml(pageType) + '" />'
            + '<button class="follow-btn' + (post.isAuthorFollowedByCurrentUser ? " following" : "") + '" type="submit">'
            + (post.isAuthorFollowedByCurrentUser ? "Bỏ theo dõi" : "Theo dõi")
            + "</button></form>";
    };

    const buildManageMenuMarkup = (post, actionWrapperClass) => {
        if (!canManagePost(post)) {
            return '<button class="' + actionWrapperClass + '" type="button" aria-label="Tùy chọn"><i class="fa-solid fa-ellipsis"></i></button>';
        }

        return '<div class="post-menu-wrap">'
            + '<button class="' + actionWrapperClass + ' post-menu-toggle" type="button" aria-label="Tùy chọn"><i class="fa-solid fa-ellipsis"></i></button>'
            + '<div class="post-menu">'
            + '<button class="post-menu-item" type="button" data-edit-post>Chỉnh sửa bài viết</button>'
            + '<form class="post-menu-form post-delete-form" action="/Home/DeletePost" method="post">'
            + '<input name="__RequestVerificationToken" type="hidden" value="' + escapeHtml(antiForgeryToken) + '" />'
            + '<input type="hidden" name="postId" value="' + escapeHtml(post.id) + '" />'
            + '<input type="hidden" name="source" value="' + escapeHtml(pageType) + '" />'
            + '<button class="post-menu-item danger" type="submit">Xóa bài viết</button>'
            + "</form></div></div>";
    };

    const buildSharedMarkup = (post) => {
        if (!post || !post.sourcePostId) {
            return "";
        }

        const sourceName = post.sourceAuthorName || "Người dùng";
        return '<div class="shared-card" data-source-post-id="' + escapeHtml(post.sourcePostId) + '">'
            + '<div class="shared-head">'
            + '<span class="avatar">' + buildAvatarMarkup(post.sourceAuthorAvatarUrl, sourceName) + "</span>"
            + "<div>"
            + '<a href="' + buildProfileUrl(post.sourceUserId || 0) + '"><strong>' + escapeHtml(sourceName) + "</strong></a>"
            + '<p style="color:var(--text-muted, var(--muted)); font-size:12px;">' + escapeHtml(formatTimeAgo(post.sourceCreatedAt || post.createdAt)) + "</p>"
            + "</div></div>"
            + '<div class="shared-body">'
            + (post.sourcePostDeleted
                ? '<div class="shared-content">Nội dung này không còn tồn tại.</div>'
                : (post.sourceContent ? '<div class="shared-content">' + escapeHtml(post.sourceContent) + "</div>" : "")
                    + (post.sourceMediaUrl ? '<div class="post-image"><img src="' + encodeURI(post.sourceMediaUrl) + '" alt="shared media" /></div>' : ""))
            + "</div></div>";
    };

    const buildMediaMarkup = (post) => {
        if (!post || post.sourcePostId || !post.mediaUrl) {
            return "";
        }

        return '<div class="post-image"><img src="' + encodeURI(post.mediaUrl) + '" alt="post image" /></div>';
    };

    const buildPostMarkup = (post) => {
        const authorName = post.authorName || "Người dùng";
        const activeLikeClass = post.isLikedByCurrentUser ? " active" : "";
        const likeIconClass = post.isLikedByCurrentUser ? "fa-solid" : "fa-regular";
        const likeLabel = post.isLikedByCurrentUser ? "Bỏ thích" : "Thích";
        const articleClass = pageType === "profile" ? "card post" : "card post-card";
        const headerClass = pageType === "profile" ? "post-top" : "post-header";
        const actionWrapperClass = pageType === "profile" ? "icon" : "icon-btn";
        const authorText = pageType === "profile"
            ? "<div><strong>" + escapeHtml(authorName) + "</strong><p>" + escapeHtml(formatTimeAgo(post.createdAt)) + ' · <i class="fa-solid fa-earth-asia"></i></p></div>'
            : "<div><h4>" + escapeHtml(authorName) + "</h4><p>" + escapeHtml(formatTimeAgo(post.createdAt)) + ' · <i class="fa-solid fa-earth-asia"></i></p></div>';

        return '<article class="' + articleClass + '" data-post-id="' + escapeHtml(post.id) + '">'
            + '<div class="' + headerClass + '">'
            + '<a class="post-author" href="' + buildProfileUrl(post.userId) + '">'
            + '<span class="avatar">' + buildAvatarMarkup(post.authorAvatarUrl, authorName) + "</span>"
            + authorText
            + "</a>"
            + '<div style="display:flex; align-items:center; gap:8px;">'
            + buildFollowMarkup(post)
            + buildManageMenuMarkup(post, actionWrapperClass)
            + "</div></div>"
            + (post.content ? '<div class="post-content">' + escapeHtml(post.content) + "</div>" : "")
            + buildSharedMarkup(post)
            + buildMediaMarkup(post)
            + '<div class="post-stats"><span data-like-count>' + escapeHtml(post.likeCount ?? 0) + ' lượt thích</span><span data-comment-count>' + escapeHtml(post.commentCount ?? 0) + " bình luận</span></div>"
            + '<div class="post-actions">'
            + '<form class="post-action-form" action="/Home/ToggleReaction" method="post">'
            + '<input name="__RequestVerificationToken" type="hidden" value="' + escapeHtml(antiForgeryToken) + '" />'
            + '<input type="hidden" name="postId" value="' + escapeHtml(post.id) + '" />'
            + '<input type="hidden" name="source" value="' + escapeHtml(pageType) + '" />'
            + '<button class="post-action-btn' + activeLikeClass + '" type="submit"><i class="' + likeIconClass + ' fa-thumbs-up"></i><span data-like-label>' + likeLabel + "</span></button>"
            + "</form>"
            + '<button class="post-action-btn" type="button" data-focus-comment="comment-' + escapeHtml(post.id) + '"><i class="fa-regular fa-comment"></i>Bình luận</button>'
            + '<form class="post-action-form" action="/Home/SharePost" method="post">'
            + '<input name="__RequestVerificationToken" type="hidden" value="' + escapeHtml(antiForgeryToken) + '" />'
            + '<input type="hidden" name="postId" value="' + escapeHtml(post.id) + '" />'
            + '<input type="hidden" name="source" value="' + escapeHtml(pageType) + '" />'
            + '<button class="post-action-btn" type="submit"><i class="fa-regular fa-paper-plane"></i>Chia sẻ</button>'
            + "</form></div>"
            + '<form class="comment-form" action="/Home/AddComment" method="post">'
            + '<input name="__RequestVerificationToken" type="hidden" value="' + escapeHtml(antiForgeryToken) + '" />'
            + '<input type="hidden" name="postId" value="' + escapeHtml(post.id) + '" />'
            + '<input type="hidden" name="source" value="' + escapeHtml(pageType) + '" />'
            + '<input id="comment-' + escapeHtml(post.id) + '" class="comment-input" type="text" name="content" maxlength="500" placeholder="Viết bình luận..." />'
            + '<button class="comment-submit" type="submit">Gửi</button>'
            + "</form></article>";
    };

    const insertRealtimePost = (post) => {
        if (!postFeed || !canRenderPostOnCurrentPage(post)) {
            return;
        }

        if (document.querySelector('[data-post-id="' + post.id + '"]')) {
            return;
        }

        const wrapper = document.createElement("div");
        wrapper.innerHTML = buildPostMarkup(post);
        const article = wrapper.firstElementChild;
        if (!article) {
            return;
        }

        const emptyState = postFeed.querySelector("[data-empty-posts]");
        const firstPost = postFeed.querySelector("[data-post-id]");
        if (firstPost) {
            postFeed.insertBefore(article, firstPost);
        } else if (emptyState) {
            postFeed.insertBefore(article, emptyState);
            emptyState.remove();
        } else {
            postFeed.appendChild(article);
        }
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

    const updatePostContent = (post) => {
        if (!post) {
            return;
        }

        const article = document.querySelector('[data-post-id="' + post.id + '"]');
        if (!article) {
            return;
        }

        let contentNode = article.querySelector(".post-content");
        const nextContent = (post.content || "").trim();
        const insertBeforeTarget = article.querySelector(".shared-card") || article.querySelector(".post-image") || article.querySelector(".post-stats");

        if (!nextContent) {
            contentNode?.remove();
            return;
        }

        if (!contentNode) {
            contentNode = document.createElement("div");
            contentNode.className = "post-content";
            if (insertBeforeTarget) {
                article.insertBefore(contentNode, insertBeforeTarget);
            } else {
                article.appendChild(contentNode);
            }
        }

        contentNode.textContent = nextContent;
    };

    const markSharedSourceDeleted = (sourcePostId) => {
        if (!sourcePostId) {
            return;
        }

        document.querySelectorAll('[data-source-post-id="' + sourcePostId + '"]').forEach((sharedCard) => {
            const body = sharedCard.querySelector(".shared-body");
            if (!body) {
                return;
            }

            body.innerHTML = '<div class="shared-content">Nội dung này không còn tồn tại.</div>';
        });
    };

    const ensureCommentList = (postId) => {
        const article = document.querySelector('[data-post-id="' + postId + '"]');
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

    const appendNotification = (payload) => {
        if (!bellList || !bellEmpty) {
            return;
        }

        bellEmpty.hidden = true;
        const item = document.createElement("button");
        item.type = "button";
        item.className = "social-notification-item unread";
        item.innerHTML = '<span class="social-notification-icon"><i class="fa-solid fa-bell"></i></span>'
            + '<span class="social-notification-copy"><strong>' + escapeHtml(payload.actorName || "Thông báo") + '</strong>'
            + '<span>' + escapeHtml(payload.message) + '</span>'
            + '<small>' + escapeHtml(formatTimeAgo(payload.createdAt)) + '</small></span>';
        item.addEventListener("click", function () {
            item.classList.remove("unread");
            if (typeof payload.postId === "number" || typeof payload.postId === "string") {
                const target = document.querySelector('[data-post-id="' + payload.postId + '"]');
                if (target) {
                    target.scrollIntoView({ behavior: "smooth", block: "center" });
                }
            }
        });
        bellList.prepend(item);
        while (bellList.children.length > 12) {
            bellList.removeChild(bellList.lastElementChild);
        }

        unreadCount += 1;
        setUnreadCount(unreadCount);
    };

    const showToast = (message) => {
        if (!toastHost || !message) {
            return;
        }

        const toast = document.createElement("div");
        toast.className = "social-toast";
        toast.textContent = message;
        toastHost.appendChild(toast);

        window.setTimeout(() => {
            toast.classList.add("hide");
            window.setTimeout(() => toast.remove(), 300);
        }, 2600);
    };

    bellButton?.addEventListener("click", function (event) {
        if (!bellDropdown) {
            return;
        }

        event.stopPropagation();
        const willOpen = !bellDropdown.classList.contains("open");
        bellDropdown.classList.toggle("open", willOpen);
        bellButton.setAttribute("aria-expanded", willOpen ? "true" : "false");
        if (willOpen) {
            unreadCount = 0;
            setUnreadCount(0);
            bellList?.querySelectorAll(".social-notification-item.unread").forEach((item) => item.classList.remove("unread"));
        }
    });

    document.addEventListener("click", function (event) {
        if (!bellDropdown || !bellButton) {
            return;
        }

        if (bellDropdown.contains(event.target) || bellButton.contains(event.target)) {
            return;
        }

        bellDropdown.classList.remove("open");
        bellButton.setAttribute("aria-expanded", "false");
    });

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(hubUrl + (hubUrl.includes("?") ? "&" : "?") + "access_token=" + encodeURIComponent(token))
        .withAutomaticReconnect()
        .build();

    connection.onreconnecting(function () {
        showToast("Đang khôi phục kết nối thông báo...");
    });

    connection.onclose(function (err) {
        console.error("[social-realtime] connection closed", err);
        showToast("Mất kết nối thông báo. Tải lại trang nếu cần.");
    });

    connection.on("PostReactionUpdated", function (payload) {
        if (!payload) {
            return;
        }

        document.querySelectorAll('[data-post-id="' + payload.postId + '"] [data-like-count]').forEach((node) => {
            node.textContent = (payload.likeCount ?? 0) + " lượt thích";
        });
    });

    connection.on("PostCommentAdded", function (payload) {
        if (!payload || !payload.comment) {
            return;
        }

        document.querySelectorAll('[data-post-id="' + payload.postId + '"] [data-comment-count]').forEach((node) => {
            node.textContent = (payload.commentCount ?? 0) + " bình luận";
        });

        const list = ensureCommentList(payload.postId);
        if (!list) {
            return;
        }

        list.querySelector("[data-comment-empty]")?.remove();

        const item = document.createElement("div");
        item.className = "comment-item";
        if (payload.comment.id) {
            item.setAttribute("data-comment-id", String(payload.comment.id));
        }

        if (payload.comment.id && list.querySelector('[data-comment-id="' + payload.comment.id + '"]')) {
            return;
        }

        item.innerHTML = '<div class="comment-meta"><a href="' + buildProfileUrl(payload.comment.userId) + '"><strong>' + escapeHtml(payload.comment.authorName) + '</strong></a>'
            + '<span>Vừa xong</span></div><div class="comment-content">' + escapeHtml(payload.comment.content) + '</div>';
        list.appendChild(item);
        while (list.children.length > 5) {
            list.removeChild(list.firstElementChild);
        }
    });

    connection.on("PostShared", function (payload) {
        if (!payload || !payload.actorName) {
            return;
        }

        if (payload.post) {
            insertRealtimePost(payload.post);
        }

        showToast(payload.actorName + " vừa chia sẻ một bài viết.");
    });

    connection.on("PostUpdated", function (payload) {
        if (!payload?.post) {
            return;
        }

        updatePostContent(payload.post);
    });

    connection.on("PostDeleted", function (payload) {
        if (!payload?.postId) {
            return;
        }

        document.querySelector('[data-post-id="' + payload.postId + '"]')?.remove();
        markSharedSourceDeleted(payload.postId);
        syncEmptyState();
    });

    connection.on("NotificationReceived", function (payload) {
        if (!payload) {
            return;
        }

        appendNotification(payload);
        showToast(payload.message);
    });

    connection.start().then(function () {
        console.info("[social-realtime] connected");
    }).catch(function (err) {
        console.error("[social-realtime] start failed", err);
        showToast("Không kết nối được thông báo realtime.");
    });
})();
