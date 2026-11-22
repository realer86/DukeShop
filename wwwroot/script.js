const API_URL = "/api/products";
const CATEGORIES_API_URL = "/api/categories";
const LOGS_API_URL = "/api/inventorylogs";
const AUTH_API_URL = "/api/auth";

const STORAGE_PRODUCTS_KEY = "DUKESHOP_PRODUCTS_V7";
const STORAGE_CATEGORIES_KEY = "DUKESHOP_CATEGORIES_V7";
const STORAGE_LOGS_KEY = "DUKESHOP_LOGS_V7";
const USERS_STORAGE_KEY = "DUKESHOP_USERS_V7";
const SESSION_TOKEN_KEY = "DUKESHOP_TOKEN_V7";
const SESSION_USER_KEY = "DUKESHOP_CURRENT_USER_V7";

const sampleCategories = [
    { id: 1, name: "Áo", description: "Áo thun, sơ mi, áo khoác thời trang" },
    { id: 2, name: "Quần", description: "Quần Jeans, kaki, quần short nam nữ" },
    { id: 3, name: "Giày dép", description: "Giày sneaker, giày thể thao & dép" },
    { id: 4, name: "Phụ kiện", description: "Balo laptop, mũ lưỡi trai, túi xách" }
];

const sampleProducts = [
    { id: "SP01", name: "Áo Thun Polo Duke Classic", categoryId: 1, category: "Áo", price: 250000, quantity: 30 },
    { id: "SP02", name: "Áo Khoác Bomber Nam Đen", categoryId: 1, category: "Áo", price: 480000, quantity: 4 },
    { id: "SP03", name: "Quần Jeans Slimfit Xanh Đậm", categoryId: 2, category: "Quần", price: 390000, quantity: 15 },
    { id: "SP04", name: "Giày Sneaker Duke Trắng", categoryId: 3, category: "Giày dép", price: 650000, quantity: 0 },
    { id: "SP05", name: "Balo Laptop Chống Nước", categoryId: 4, category: "Phụ kiện", price: 320000, quantity: 12 },
    { id: "SP06", name: "Áo Sơ Mi Oxford Trắng", categoryId: 1, category: "Áo", price: 290000, quantity: 22 },
    { id: "SP07", name: "Quần Short Kaki Basic", categoryId: 2, category: "Quần", price: 210000, quantity: 3 },
    { id: "SP08", name: "Mũ Lưỡi Trai Thêu Logo Duke", categoryId: 4, category: "Phụ kiện", price: 150000, quantity: 18 }
];

const defaultUsers = [
    { id: 1, fullName: "Lê Gia Hoàng", username: "quanly", email: "quanly@dukeshop.vn", phone: "0901234567", password: "DukeShop@2026", role: "ADMIN" },
    { id: 2, fullName: "Nguyễn Văn A", username: "nhanvien01", email: "nhanvien01@dukeshop.vn", phone: "0987654321", password: "NhanVien@123", role: "STAFF" }
];

let products = [];
let categories = [];
let inventoryLogs = [];
let isUsingBackend = true;

let currentPage = 1;
const pageSize = 5;
let currentRole = "STAFF";
let currentUser = null;
let authToken = null;
let currentView = "PRODUCTS";

window.addEventListener("DOMContentLoaded", function () {
    const tabLoginBtn = document.getElementById("tab-login");
    const tabRegisterBtn = document.getElementById("tab-register");
    const tabForgotBtn = document.getElementById("tab-forgot");
    if (tabLoginBtn) tabLoginBtn.addEventListener("click", () => switchAuthTab("LOGIN"));
    if (tabRegisterBtn) tabRegisterBtn.addEventListener("click", () => switchAuthTab("REGISTER"));
    if (tabForgotBtn) tabForgotBtn.addEventListener("click", () => switchAuthTab("FORGOT"));

    initLocalFallbacks();
    checkExistingSession();
});

function initLocalFallbacks() {
    if (!localStorage.getItem(USERS_STORAGE_KEY)) {
        localStorage.setItem(USERS_STORAGE_KEY, JSON.stringify(defaultUsers));
    }
    if (!localStorage.getItem(STORAGE_CATEGORIES_KEY)) {
        localStorage.setItem(STORAGE_CATEGORIES_KEY, JSON.stringify(sampleCategories));
    }
    if (!localStorage.getItem(STORAGE_PRODUCTS_KEY)) {
        localStorage.setItem(STORAGE_PRODUCTS_KEY, JSON.stringify(sampleProducts));
    }
    if (!localStorage.getItem(STORAGE_LOGS_KEY)) {
        localStorage.setItem(STORAGE_LOGS_KEY, JSON.stringify([]));
    }
}

function getAuthHeaders(includeJson = true) {
    const headers = {};
    if (includeJson) headers["Content-Type"] = "application/json";
    if (authToken) headers["Authorization"] = `Bearer ${authToken}`;
    return headers;
}

function switchView(viewName) {
    currentView = viewName;

    document.getElementById("view-products").style.display = (viewName === "PRODUCTS") ? "block" : "none";
    document.getElementById("view-inventory").style.display = (viewName === "INVENTORY") ? "block" : "none";
    document.getElementById("view-profile").style.display = (viewName === "PROFILE") ? "block" : "none";

    document.getElementById("nav-products").classList.toggle("active", viewName === "PRODUCTS");
    document.getElementById("nav-inventory").classList.toggle("active", viewName === "INVENTORY");
    document.getElementById("nav-profile").classList.toggle("active", viewName === "PROFILE");

    const bc = document.getElementById("breadcrumb-current");
    const title = document.getElementById("page-main-title");

    if (viewName === "PRODUCTS") {
        bc.innerText = "Quản lý sản phẩm";
        title.innerText = "Quản lý kho hàng";
    } else if (viewName === "INVENTORY") {
        bc.innerText = "Danh mục & Lịch sử kho";
        title.innerText = "Danh mục & Nhật ký nhập xuất kho";
        loadCategoriesAndLogs();
    } else if (viewName === "PROFILE") {
        bc.innerText = "Hồ sơ cá nhân";
        title.innerText = "Hồ sơ cá nhân";
        populateProfileForm();
    }
}

function checkExistingSession() {
    const savedSession = localStorage.getItem(SESSION_USER_KEY);
    const savedToken = localStorage.getItem(SESSION_TOKEN_KEY);

    if (savedSession && savedToken) {
        currentUser = JSON.parse(savedSession);
        authToken = savedToken;
        applyUserSession(currentUser, false);
        closeAuthModal();
        loadAllData();
    } else {
        openAuthModal();
    }
}

function openAuthModal() {
    document.getElementById("auth-overlay").style.display = "flex";
}

function closeAuthModal() {
    document.getElementById("auth-overlay").style.display = "none";
}

function clearAuthErrors() {
    const errIds = [
        "err-login-username", "err-login-password",
        "err-reg-fullname", "err-reg-username", "err-reg-email", "err-reg-password", "err-reg-confirm",
        "err-forgot-username", "err-forgot-fullname", "err-forgot-newpassword", "err-forgot-confirm"
    ];
    errIds.forEach(id => {
        const el = document.getElementById(id);
        if (el) el.innerText = "";
    });
}

document.getElementById("login-form").addEventListener("submit", async function (e) {
    e.preventDefault();
    clearAuthErrors();

    const username = document.getElementById("login-username").value.trim();
    const password = document.getElementById("login-password").value;

    let valid = true;
    if (!username) {
        document.getElementById("err-login-username").innerText = "Vui lòng nhập tên đăng nhập!";
        valid = false;
    }
    if (!password || password.length < 8) {
        document.getElementById("err-login-password").innerText = "Mật khẩu phải có từ 8 ký tự trở lên!";
        valid = false;
    }
    if (!valid) return;

    try {
        const res = await fetch(`${AUTH_API_URL}/login`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ username, password })
        });

        if (res.ok) {
            const data = await res.json();
            authToken = data.token;
            localStorage.setItem(SESSION_TOKEN_KEY, authToken);
            applyUserSession(data.user, true);
            closeAuthModal();
            await loadAllData();
            return;
        } else if (res.status === 401 || res.status === 400) {
            const err = await res.json();
            document.getElementById("err-login-password").innerText = err.message || "Sai tên đăng nhập hoặc mật khẩu!";
            return;
        }
        throw new Error("Offline");
    } catch (err) {
        const localUsers = JSON.parse(localStorage.getItem(USERS_STORAGE_KEY) || "[]");
        const found = localUsers.find(
            u => u.username.toLowerCase() === username.toLowerCase() && u.password === password
        );
        if (!found) {
            document.getElementById("err-login-password").innerText = "Sai tên đăng nhập hoặc mật khẩu!";
            return;
        }
        authToken = "LOCAL_SESSION_TOKEN";
        localStorage.setItem(SESSION_TOKEN_KEY, authToken);
        applyUserSession(found, true);
        closeAuthModal();
        await loadAllData();
    }
});

document.getElementById("register-form").addEventListener("submit", async function (e) {
    e.preventDefault();
    clearAuthErrors();

    const fullName = document.getElementById("reg-fullname").value.trim();
    const username = document.getElementById("reg-username").value.trim();
    const email = document.getElementById("reg-email").value.trim();
    const phone = document.getElementById("reg-phone").value.trim();
    const password = document.getElementById("reg-password").value;
    const confirmPass = document.getElementById("reg-confirm").value;

    let valid = true;
    if (!fullName) {
        document.getElementById("err-reg-fullname").innerText = "Vui lòng nhập họ và tên!";
        valid = false;
    }
    if (username.length < 3) {
        document.getElementById("err-reg-username").innerText = "Tên đăng nhập phải từ 3 ký tự trở lên!";
        valid = false;
    }
    if (username.toLowerCase() === "quanly" || username.toLowerCase() === "admin") {
        document.getElementById("err-reg-username").innerText = "Tên đăng nhập không khả dụng!";
        valid = false;
    }
    if (email && !email.includes("@")) {
        document.getElementById("err-reg-email").innerText = "Địa chỉ email không hợp lệ!";
        valid = false;
    }
    if (password.length < 8) {
        document.getElementById("err-reg-password").innerText = "Mật khẩu phải từ 8 ký tự trở lên!";
        valid = false;
    }
    if (confirmPass !== password) {
        document.getElementById("err-reg-confirm").innerText = "Mật khẩu xác nhận không khớp!";
        valid = false;
    }
    if (!valid) return;

    const payload = { fullName, username, email, phone, password };

    try {
        const res = await fetch(`${AUTH_API_URL}/register`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        if (res.ok) {
            const data = await res.json();
            authToken = data.token;
            localStorage.setItem(SESSION_TOKEN_KEY, authToken);
            showToast("Đăng ký tài khoản thành công.");
            document.getElementById("register-form").reset();
            applyUserSession(data.user, false);
            closeAuthModal();
            await loadAllData();
            return;
        } else if (res.status === 409 || res.status === 400) {
            const err = await res.json();
            document.getElementById("err-reg-username").innerText = err.message || "Tên đăng nhập đã tồn tại!";
            return;
        }
        throw new Error("Offline");
    } catch (err) {
        const localUsers = JSON.parse(localStorage.getItem(USERS_STORAGE_KEY) || "[]");
        if (localUsers.some(u => u.username.toLowerCase() === username.toLowerCase())) {
            document.getElementById("err-reg-username").innerText = "Tên đăng nhập này đã tồn tại!";
            return;
        }
        const newLocalUser = { id: localUsers.length + 1, fullName, username, email, phone, password, role: "STAFF" };
        localUsers.push(newLocalUser);
        localStorage.setItem(USERS_STORAGE_KEY, JSON.stringify(localUsers));
        authToken = "LOCAL_SESSION_TOKEN";
        localStorage.setItem(SESSION_TOKEN_KEY, authToken);
        showToast("Đăng ký tài khoản thành công.");
        document.getElementById("register-form").reset();
        applyUserSession(newLocalUser, false);
        closeAuthModal();
        await loadAllData();
    }
});

document.getElementById("forgot-form").addEventListener("submit", async function (e) {
    e.preventDefault();
    clearAuthErrors();

    const username = document.getElementById("forgot-username").value.trim();
    const fullName = document.getElementById("forgot-fullname").value.trim();
    const newPassword = document.getElementById("forgot-newpassword").value;
    const confirmPass = document.getElementById("forgot-confirm").value;

    let valid = true;
    if (!username) {
        document.getElementById("err-forgot-username").innerText = "Vui lòng nhập tên đăng nhập!";
        valid = false;
    }
    if (!fullName) {
        document.getElementById("err-forgot-fullname").innerText = "Vui lòng nhập họ tên xác minh!";
        valid = false;
    }
    if (newPassword.length < 8) {
        document.getElementById("err-forgot-newpassword").innerText = "Mật khẩu mới phải từ 8 ký tự trở lên!";
        valid = false;
    }
    if (confirmPass !== newPassword) {
        document.getElementById("err-forgot-confirm").innerText = "Mật khẩu xác nhận không khớp!";
        valid = false;
    }
    if (!valid) return;

    try {
        const res = await fetch(`${AUTH_API_URL}/forgot-password`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ username, fullName, newPassword })
        });
        const data = await res.json();
        if (res.ok) {
            showToast(data.message || "Đặt lại mật khẩu thành công.");
            document.getElementById("forgot-form").reset();
            switchAuthTab("LOGIN");
            document.getElementById("login-username").value = username;
            return;
        }
        document.getElementById("err-forgot-username").innerText = data.message || "Thông tin xác minh chưa khớp!";
    } catch (err) {
        const localUsers = JSON.parse(localStorage.getItem(USERS_STORAGE_KEY) || "[]");
        const idx = localUsers.findIndex(u => u.username.toLowerCase() === username.toLowerCase());
        if (idx === -1) {
            document.getElementById("err-forgot-username").innerText = "Không tìm thấy tài khoản!";
            return;
        }
        if (localUsers[idx].role === "ADMIN") {
            document.getElementById("err-forgot-username").innerText = "Tài khoản Quản lý không hỗ trợ đặt lại công khai!";
            return;
        }
        if (localUsers[idx].fullName.trim().toLowerCase() !== fullName.toLowerCase()) {
            document.getElementById("err-forgot-fullname").innerText = "Họ tên xác minh không khớp!";
            return;
        }
        localUsers[idx].password = newPassword;
        localStorage.setItem(USERS_STORAGE_KEY, JSON.stringify(localUsers));
        showToast("Đặt lại mật khẩu thành công.");
        document.getElementById("forgot-form").reset();
        switchAuthTab("LOGIN");
    }
});

async function logoutUser() {
    if (isUsingBackend && authToken && authToken !== "LOCAL_SESSION_TOKEN") {
        try {
            await fetch(`${AUTH_API_URL}/logout`, {
                method: "POST",
                headers: getAuthHeaders(false)
            });
        } catch (e) { }
    }
    localStorage.removeItem(SESSION_USER_KEY);
    localStorage.removeItem(SESSION_TOKEN_KEY);
    currentUser = null;
    authToken = null;
    document.getElementById("login-form").reset();
    document.getElementById("register-form").reset();
    document.getElementById("forgot-form").reset();
    switchView("PRODUCTS");
    switchAuthTab("LOGIN");
    openAuthModal();
    showToast("Đã đăng xuất khỏi hệ thống.");
}

function applyUserSession(user, showWelcomeToast) {
    currentUser = user;
    currentRole = user.role;
    localStorage.setItem(SESSION_USER_KEY, JSON.stringify(user));

    document.getElementById("user-display-name").innerText = user.fullName;

    const submitBtn = document.getElementById("btn-submit");
    const resetBtn = document.getElementById("btn-reset-data");
    const staffBanner = document.getElementById("staff-notice-banner");
    const formCard = document.getElementById("form-card");
    const contentLayout = document.getElementById("products-content-layout");
    const thActions = document.getElementById("th-actions");
    const rolePill = document.getElementById("header-role-pill");
    const roleText = document.getElementById("header-role-text");

    const catForm = document.getElementById("category-form");
    const stockCard = document.getElementById("stock-adjust-card");
    const thCatActions = document.getElementById("th-cat-actions");
    const invGrid = document.querySelector("#view-inventory .two-col-grid");

    if (currentRole === "ADMIN") {
        document.getElementById("user-avatar").innerText = "QL";
        document.getElementById("user-role-badge").innerText = `@${user.username}`;
        rolePill.classList.remove("staff-mode");
        roleText.innerText = "Quản lý";

        submitBtn.disabled = false;
        formCard.style.display = "block";
        contentLayout.classList.remove("staff-only-view");
        thActions.style.display = "";
        resetBtn.style.display = "inline-flex";
        staffBanner.style.display = "none";

        if (catForm) catForm.style.display = "block";
        if (stockCard) stockCard.style.display = "block";
        if (thCatActions) thCatActions.style.display = "";
        if (invGrid) invGrid.classList.remove("single-col");

        if (showWelcomeToast) showToast(`Xin chào, ${user.fullName}.`);
    } else {
        document.getElementById("user-avatar").innerText = "NV";
        document.getElementById("user-role-badge").innerText = `@${user.username}`;
        rolePill.classList.add("staff-mode");
        roleText.innerText = "Nhân viên";

        submitBtn.disabled = true;
        formCard.style.display = "none";
        contentLayout.classList.add("staff-only-view");
        thActions.style.display = "none";
        resetBtn.style.display = "none";
        staffBanner.style.display = "flex";

        if (catForm) catForm.style.display = "none";
        if (stockCard) stockCard.style.display = "none";
        if (thCatActions) thCatActions.style.display = "none";
        if (invGrid) invGrid.classList.add("single-col");

        resetForm();
        if (showWelcomeToast) showToast(`Xin chào, ${user.fullName}.`);
    }

    populateProfileForm();
}

function populateProfileForm() {
    if (!currentUser) return;
    document.getElementById("prof-username").value = currentUser.username || "";
    document.getElementById("prof-fullname").value = currentUser.fullName || "";
    document.getElementById("prof-email").value = currentUser.email || "";
    document.getElementById("prof-phone").value = currentUser.phone || "";
    document.getElementById("profile-role-pill").innerText =
        currentUser.role === "ADMIN" ? "Quản lý" : "Nhân viên";
}

document.getElementById("profile-form").addEventListener("submit", async function (e) {
    e.preventDefault();
    document.getElementById("err-prof-fullname").innerText = "";
    document.getElementById("err-prof-email").innerText = "";

    const fullName = document.getElementById("prof-fullname").value.trim();
    const email = document.getElementById("prof-email").value.trim();
    const phone = document.getElementById("prof-phone").value.trim();

    if (!fullName) {
        document.getElementById("err-prof-fullname").innerText = "Họ và tên không được để trống!";
        return;
    }
    if (email && !email.includes("@")) {
        document.getElementById("err-prof-email").innerText = "Email không hợp lệ!";
        return;
    }

    if (isUsingBackend) {
        const res = await fetch(`${AUTH_API_URL}/profile`, {
            method: "PUT",
            headers: getAuthHeaders(true),
            body: JSON.stringify({ fullName, email, phone })
        });
        if (res.status === 401) { logoutUser(); return; }
        const data = await res.json();
        if (!res.ok) {
            showToast(data.message || "Không thể cập nhật hồ sơ.");
            return;
        }
        if (data.token) {
            authToken = data.token;
            localStorage.setItem(SESSION_TOKEN_KEY, authToken);
        }
        applyUserSession(data.user, false);
        showToast("Đã cập nhật thông tin cá nhân.");
    } else {
        currentUser.fullName = fullName;
        currentUser.email = email;
        currentUser.phone = phone;
        applyUserSession(currentUser, false);
        showToast("Đã cập nhật thông tin cá nhân.");
    }
});

document.getElementById("change-password-form").addEventListener("submit", async function (e) {
    e.preventDefault();
    document.getElementById("err-cp-current").innerText = "";
    document.getElementById("err-cp-new").innerText = "";
    document.getElementById("err-cp-confirm").innerText = "";

    const currentPassword = document.getElementById("cp-current").value;
    const newPassword = document.getElementById("cp-new").value;
    const confirmPassword = document.getElementById("cp-confirm").value;

    let valid = true;
    if (!currentPassword) {
        document.getElementById("err-cp-current").innerText = "Vui lòng nhập mật khẩu hiện tại!";
        valid = false;
    }
    if (newPassword.length < 8) {
        document.getElementById("err-cp-new").innerText = "Mật khẩu mới phải có từ 8 ký tự trở lên!";
        valid = false;
    }
    if (confirmPassword !== newPassword) {
        document.getElementById("err-cp-confirm").innerText = "Mật khẩu xác nhận không khớp!";
        valid = false;
    }
    if (!valid) return;

    if (isUsingBackend) {
        const res = await fetch(`${AUTH_API_URL}/change-password`, {
            method: "PUT",
            headers: getAuthHeaders(true),
            body: JSON.stringify({ currentPassword, newPassword })
        });
        if (res.status === 401) { logoutUser(); return; }
        const data = await res.json();
        if (!res.ok) {
            document.getElementById("err-cp-current").innerText = data.message || "Đổi mật khẩu thất bại!";
            return;
        }
        document.getElementById("change-password-form").reset();
        showToast("Đã cập nhật mật khẩu mới.");
    } else {
        showToast("Đã cập nhật mật khẩu mới.");
        document.getElementById("change-password-form").reset();
    }
});

async function loadAllData() {
    await loadCategoriesAndLogs();
    await loadProducts();
}

async function loadProducts() {
    const badge = document.getElementById("connection-badge");
    try {
        const res = await fetch(API_URL, { headers: getAuthHeaders(false) });
        if (res.status === 401) {
            logoutUser();
            return;
        }
        if (!res.ok) throw new Error("Offline");
        products = await res.json();
        isUsingBackend = true;
        badge.innerHTML = `<span class="dot"></span> Trực tuyến`;
    } catch (error) {
        isUsingBackend = false;
        badge.innerHTML = `<span class="dot"></span> Ngoại tuyến`;
        products = JSON.parse(localStorage.getItem(STORAGE_PRODUCTS_KEY) || "[]");
    }
    renderProducts();
    populateAdjustProductDropdown();
}

async function loadCategoriesAndLogs() {
    try {
        const [catRes, logRes] = await Promise.all([
            fetch(CATEGORIES_API_URL, { headers: getAuthHeaders(false) }),
            fetch(LOGS_API_URL, { headers: getAuthHeaders(false) })
        ]);
        if (catRes.status === 401 || logRes.status === 401) {
            logoutUser();
            return;
        }
        if (!catRes.ok || !logRes.ok) throw new Error("Offline");
        categories = await catRes.json();
        inventoryLogs = await logRes.json();
        isUsingBackend = true;
    } catch (e) {
        categories = JSON.parse(localStorage.getItem(STORAGE_CATEGORIES_KEY) || "[]");
        inventoryLogs = JSON.parse(localStorage.getItem(STORAGE_LOGS_KEY) || "[]");
    }
    populateCategorySelects();
    renderCategoriesTable();
    renderLogsTable();
}

function populateCategorySelects() {
    const prodSelect = document.getElementById("product-category");
    const filterSelect = document.getElementById("filter-category");
    const currentProdVal = prodSelect.value;
    const currentFilterVal = filterSelect.value;

    prodSelect.innerHTML = `<option value="">-- Chọn danh mục --</option>`;
    filterSelect.innerHTML = `<option value="ALL">Tất cả danh mục</option>`;

    categories.forEach(cat => {
        prodSelect.innerHTML += `<option value="${cat.id}">${cat.name}</option>`;
        filterSelect.innerHTML += `<option value="${cat.id}">${cat.name}</option>`;
    });

    if (currentProdVal) prodSelect.value = currentProdVal;
    if (currentFilterVal) filterSelect.value = currentFilterVal;
}

function populateAdjustProductDropdown() {
    const sel = document.getElementById("adjust-product");
    if (!sel) return;
    sel.innerHTML = `<option value="">-- Chọn sản phẩm trong kho --</option>`;
    products.forEach(p => {
        sel.innerHTML += `<option value="${p.id}">${p.id} - ${p.name} (Tồn: ${p.quantity})</option>`;
    });
}

function validateForm(name, categoryId, price, quantity) {
    let isValid = true;

    document.getElementById("err-name").innerText = "";
    document.getElementById("err-category").innerText = "";
    document.getElementById("err-price").innerText = "";
    document.getElementById("err-quantity").innerText = "";

    document.getElementById("product-name").classList.remove("input-error");
    document.getElementById("product-category").classList.remove("input-error");
    document.getElementById("product-price").classList.remove("input-error");
    document.getElementById("product-quantity").classList.remove("input-error");

    if (name === "") {
        document.getElementById("err-name").innerText = "Vui lòng nhập tên sản phẩm!";
        document.getElementById("product-name").classList.add("input-error");
        isValid = false;
    }
    if (!categoryId || isNaN(categoryId)) {
        document.getElementById("err-category").innerText = "Vui lòng chọn danh mục!";
        document.getElementById("product-category").classList.add("input-error");
        isValid = false;
    }
    if (isNaN(price) || price <= 0) {
        document.getElementById("err-price").innerText = "Giá bán phải lớn hơn 0!";
        document.getElementById("product-price").classList.add("input-error");
        isValid = false;
    }
    if (isNaN(quantity) || quantity < 0) {
        document.getElementById("err-quantity").innerText = "Số lượng tồn kho không được âm!";
        document.getElementById("product-quantity").classList.add("input-error");
        isValid = false;
    }

    return isValid;
}

document.getElementById("product-form").addEventListener("submit", async function (event) {
    event.preventDefault();

    if (currentRole !== "ADMIN") {
        showToast("Bạn không có quyền thêm hoặc chỉnh sửa sản phẩm.");
        return;
    }

    const idInput = document.getElementById("product-id").value;
    const nameInput = document.getElementById("product-name").value.trim();
    const categoryIdInput = Number(document.getElementById("product-category").value);
    const priceInput = Number(document.getElementById("product-price").value);
    const quantityInput = Number(document.getElementById("product-quantity").value);

    if (!validateForm(nameInput, categoryIdInput, priceInput, quantityInput)) return;

    const payload = {
        name: nameInput,
        categoryId: categoryIdInput,
        price: priceInput,
        quantity: quantityInput
    };

    if (isUsingBackend) {
        const url = idInput === "" ? API_URL : `${API_URL}/${idInput}`;
        const method = idInput === "" ? "POST" : "PUT";

        const res = await fetch(url, {
            method,
            headers: getAuthHeaders(true),
            body: JSON.stringify(payload)
        });

        if (res.status === 401) { logoutUser(); return; }
        if (!res.ok) {
            const err = await res.json();
            showToast(err.message || "Có lỗi xảy ra khi lưu sản phẩm.");
            return;
        }
        showToast(idInput === "" ? "Đã thêm sản phẩm mới." : "Đã cập nhật sản phẩm.");
        await loadAllData();
    } else {
        const catObj = categories.find(c => c.id === categoryIdInput);
        const catName = catObj ? catObj.name : "Khác";
        if (idInput === "") {
            let maxNum = 0;
            products.forEach(item => {
                const n = parseInt(item.id.substring(2));
                if (n > maxNum) maxNum = n;
            });
            const next = maxNum + 1;
            products.push({ id: next < 10 ? "SP0" + next : "SP" + next, ...payload, category: catName });
            showToast("Đã thêm sản phẩm mới.");
        } else {
            const idx = products.findIndex(item => item.id === idInput);
            if (idx !== -1) products[idx] = { id: idInput, ...payload, category: catName };
            showToast("Đã cập nhật sản phẩm.");
        }
        localStorage.setItem(STORAGE_PRODUCTS_KEY, JSON.stringify(products));
        renderProducts();
    }

    resetForm();
});

function editProduct(id) {
    if (currentRole !== "ADMIN") return;
    const product = products.find(item => item.id === id);
    if (!product) return;

    document.getElementById("product-id").value = product.id;
    document.getElementById("product-name").value = product.name;
    document.getElementById("product-category").value = product.categoryId;
    document.getElementById("product-price").value = product.price;
    document.getElementById("product-quantity").value = product.quantity;

    document.getElementById("form-title").innerHTML = `<i class="fa-solid fa-pen-to-square"></i> Chỉnh Sửa (${product.id})`;
    document.getElementById("btn-submit").innerHTML = `<i class="fa-solid fa-rotate"></i> Cập nhật`;
    document.getElementById("btn-cancel").style.display = "inline-flex";
}

async function deleteProduct(id) {
    if (currentRole !== "ADMIN") return;
    const product = products.find(item => item.id === id);
    if (!product) return;

    if (confirm(`Bạn có chắc chắn muốn xóa "${product.name}" khỏi hệ thống không?`)) {
        if (isUsingBackend) {
            const res = await fetch(`${API_URL}/${id}`, {
                method: "DELETE",
                headers: getAuthHeaders(false)
            });
            if (res.status === 401) { logoutUser(); return; }
            if (!res.ok) {
                const err = await res.json();
                showToast(err.message || "Không thể xóa sản phẩm.");
                return;
            }
            await loadAllData();
        } else {
            products = products.filter(item => item.id !== id);
            localStorage.setItem(STORAGE_PRODUCTS_KEY, JSON.stringify(products));
            renderProducts();
        }
        showToast("Đã xóa sản phẩm khỏi kho.");
    }
}

function resetForm() {
    document.getElementById("product-form").reset();
    document.getElementById("product-id").value = "";
    document.getElementById("form-title").innerHTML = `<i class="fa-solid fa-circle-plus"></i> Thêm Sản Phẩm Mới`;
    document.getElementById("btn-submit").innerHTML = `<i class="fa-solid fa-floppy-disk"></i> Lưu sản phẩm`;
    document.getElementById("btn-cancel").style.display = "none";

    ["err-name", "err-category", "err-price", "err-quantity"].forEach(id => {
        document.getElementById(id).innerText = "";
    });
    ["product-name", "product-category", "product-price", "product-quantity"].forEach(id => {
        document.getElementById(id).classList.remove("input-error");
    });
}

function handleFilterChange() {
    currentPage = 1;
    renderProducts();
}

function renderProducts() {
    const tbody = document.getElementById("product-tbody");
    const keyword = document.getElementById("search-input").value.trim().toLowerCase();
    const selectedCategory = document.getElementById("filter-category").value;
    const sortBy = document.getElementById("sort-select").value;

    let filteredList = products.filter(item => {
        const matchName = item.name.toLowerCase().includes(keyword) || item.id.toLowerCase().includes(keyword);
        const matchCategory = (selectedCategory === "ALL") || (String(item.categoryId) === String(selectedCategory));
        return matchName && matchCategory;
    });

    if (sortBy === "PRICE_ASC") filteredList.sort((a, b) => a.price - b.price);
    else if (sortBy === "PRICE_DESC") filteredList.sort((a, b) => b.price - a.price);
    else if (sortBy === "NAME_ASC") filteredList.sort((a, b) => a.name.localeCompare(b.name));
    else if (sortBy === "STOCK_ASC") filteredList.sort((a, b) => a.quantity - b.quantity);

    const totalItems = filteredList.length;
    const totalPages = Math.ceil(totalItems / pageSize) || 1;
    if (currentPage > totalPages) currentPage = totalPages;

    const startIndex = (currentPage - 1) * pageSize;
    const pagedProducts = filteredList.slice(startIndex, startIndex + pageSize);

    document.getElementById("table-count-label").innerText = `Tổng: ${totalItems} mặt hàng`;
    tbody.innerHTML = "";

    const colSpanCount = currentRole === "ADMIN" ? 8 : 7;

    if (pagedProducts.length === 0) {
        tbody.innerHTML = `<tr><td colspan="${colSpanCount}" style="text-align:center; padding: 28px; color: #64748b;">Không tìm thấy sản phẩm nào phù hợp.</td></tr>`;
    } else {
        pagedProducts.forEach(item => {
            const totalItemValue = item.price * item.quantity;
            let statusBadge = `<span class="badge badge-ok">● Còn hàng</span>`;
            if (item.quantity === 0) statusBadge = `<span class="badge badge-out">● Hết hàng</span>`;
            else if (item.quantity < 5) statusBadge = `<span class="badge badge-low">● Sắp hết</span>`;

            const actionCellHtml = currentRole === "ADMIN"
                ? `<td style="text-align: right;">
                    <button class="btn btn-edit" onclick="editProduct('${item.id}')">
                        <i class="fa-solid fa-pen"></i> Sửa
                    </button>
                    <button class="btn btn-delete" onclick="deleteProduct('${item.id}')">
                        <i class="fa-solid fa-trash"></i> Xóa
                    </button>
                </td>`
                : "";

            const tr = document.createElement("tr");
            tr.innerHTML = `
                <td><span class="id-pill">${item.id}</span></td>
                <td><strong>${item.name}</strong></td>
                <td><span class="cat-pill">${item.category}</span></td>
                <td>${item.price.toLocaleString("vi-VN")} ₫</td>
                <td><strong>${item.quantity}</strong></td>
                <td><strong style="color: #4f46e5;">${totalItemValue.toLocaleString("vi-VN")} ₫</strong></td>
                <td>${statusBadge}</td>
                ${actionCellHtml}
            `;
            tbody.appendChild(tr);
        });
    }

    renderPagination(totalItems, totalPages, startIndex);
    updateDashboardStats();
}

function renderPagination(totalItems, totalPages, startIndex) {
    const infoText = document.getElementById("page-info-text");
    const controls = document.getElementById("pagination-controls");

    if (totalItems === 0) {
        infoText.innerText = "Không có sản phẩm nào";
        controls.innerHTML = "";
        return;
    }

    const showStart = startIndex + 1;
    const showEnd = Math.min(startIndex + pageSize, totalItems);
    infoText.innerText = `Hiển thị ${showStart} - ${showEnd} trên tổng số ${totalItems} sản phẩm (Trang ${currentPage}/${totalPages})`;

    let html = `<button class="page-btn" ${currentPage === 1 ? "disabled" : ""} onclick="goToPage(${currentPage - 1})">« Trước</button>`;
    for (let i = 1; i <= totalPages; i++) {
        html += `<button class="page-btn ${i === currentPage ? "active" : ""}" onclick="goToPage(${i})">${i}</button>`;
    }
    html += `<button class="page-btn" ${currentPage === totalPages ? "disabled" : ""} onclick="goToPage(${currentPage + 1})">Sau »</button>`;
    controls.innerHTML = html;
}

function goToPage(page) {
    currentPage = page;
    renderProducts();
}

function updateDashboardStats() {
    let totalProducts = products.length;
    let totalStock = 0;
    let totalValue = 0;
    let lowStockCount = 0;

    products.forEach(item => {
        totalStock += item.quantity;
        totalValue += item.price * item.quantity;
        if (item.quantity < 5) lowStockCount++;
    });

    document.getElementById("stat-total-products").innerText = totalProducts;
    document.getElementById("stat-total-stock").innerText = totalStock;
    document.getElementById("stat-total-value").innerText = totalValue.toLocaleString("vi-VN") + " ₫";
    document.getElementById("stat-low-stock").innerText = lowStockCount;
}

function renderCategoriesTable() {
    const tbody = document.getElementById("category-tbody");
    if (!tbody) return;
    tbody.innerHTML = "";

    const colSpan = currentRole === "ADMIN" ? 6 : 5;
    if (categories.length === 0) {
        tbody.innerHTML = `<tr><td colspan="${colSpan}" style="text-align:center; padding: 20px; color:#64748b;">Chưa có danh mục nào.</td></tr>`;
        return;
    }

    categories.forEach(cat => {
        const count = cat.productCount !== undefined
            ? cat.productCount
            : products.filter(p => p.categoryId === cat.id).length;
        const stock = cat.totalStock !== undefined
            ? cat.totalStock
            : products.filter(p => p.categoryId === cat.id).reduce((s, p) => s + p.quantity, 0);
        const val = cat.totalValue !== undefined
            ? cat.totalValue
            : products.filter(p => p.categoryId === cat.id).reduce((s, p) => s + p.price * p.quantity, 0);

        const actionHtml = currentRole === "ADMIN"
            ? `<td style="text-align:right;">
                <button class="btn btn-delete" onclick="deleteCategory(${cat.id}, '${cat.name}')">
                    <i class="fa-solid fa-trash"></i> Xóa
                </button>
              </td>`
            : "";

        const tr = document.createElement("tr");
        tr.innerHTML = `
            <td><span class="id-pill">#${cat.id}</span></td>
            <td><strong>${cat.name}</strong><div style="font-size:11.5px; color:#64748b;">${cat.description || ""}</div></td>
            <td><strong>${count}</strong></td>
            <td>${stock}</td>
            <td><strong style="color:#059669;">${Number(val).toLocaleString("vi-VN")} ₫</strong></td>
            ${actionHtml}
        `;
        tbody.appendChild(tr);
    });
}

document.getElementById("category-form").addEventListener("submit", async function (e) {
    e.preventDefault();
    if (currentRole !== "ADMIN") return;
    document.getElementById("err-cat-name").innerText = "";

    const name = document.getElementById("cat-name").value.trim();
    const description = document.getElementById("cat-desc").value.trim();

    if (!name) {
        document.getElementById("err-cat-name").innerText = "Vui lòng nhập tên danh mục!";
        return;
    }

    if (isUsingBackend) {
        const res = await fetch(CATEGORIES_API_URL, {
            method: "POST",
            headers: getAuthHeaders(true),
            body: JSON.stringify({ name, description })
        });
        if (res.status === 401) { logoutUser(); return; }
        const data = await res.json();
        if (!res.ok) {
            document.getElementById("err-cat-name").innerText = data.message || "Không thể thêm danh mục!";
            return;
        }
        document.getElementById("category-form").reset();
        showToast("Đã thêm danh mục mới.");
        await loadCategoriesAndLogs();
    }
});

async function deleteCategory(id, name) {
    if (currentRole !== "ADMIN") return;
    if (!confirm(`Bạn có chắc muốn xóa danh mục "${name}"?`)) return;

    if (isUsingBackend) {
        const res = await fetch(`${CATEGORIES_API_URL}/${id}`, {
            method: "DELETE",
            headers: getAuthHeaders(false)
        });
        const data = await res.json();
        if (!res.ok) {
            showToast(data.message || "Không thể xóa danh mục đang có sản phẩm.");
            return;
        }
        showToast("Đã xóa danh mục.");
        await loadCategoriesAndLogs();
    }
}

document.getElementById("stock-adjust-form").addEventListener("submit", async function (e) {
    e.preventDefault();
    if (currentRole !== "ADMIN") return;

    document.getElementById("err-adjust-product").innerText = "";
    document.getElementById("err-adjust-qty").innerText = "";

    const productId = document.getElementById("adjust-product").value;
    const actionType = document.getElementById("adjust-type").value;
    const quantity = Number(document.getElementById("adjust-qty").value);
    const note = document.getElementById("adjust-note").value.trim();

    let valid = true;
    if (!productId) {
        document.getElementById("err-adjust-product").innerText = "Vui lòng chọn sản phẩm!";
        valid = false;
    }
    if (isNaN(quantity) || quantity <= 0) {
        document.getElementById("err-adjust-qty").innerText = "Số lượng phải lớn hơn 0!";
        valid = false;
    }
    if (!valid) return;

    if (isUsingBackend) {
        const res = await fetch(`${LOGS_API_URL}/adjust`, {
            method: "POST",
            headers: getAuthHeaders(true),
            body: JSON.stringify({ productId, actionType, quantity, note })
        });
        if (res.status === 401) { logoutUser(); return; }
        const data = await res.json();
        if (!res.ok) {
            showToast(data.message || "Không thể ghi phiếu kho.");
            return;
        }
        document.getElementById("stock-adjust-form").reset();
        showToast(data.message);
        await loadAllData();
    }
});

function renderLogsTable() {
    const tbody = document.getElementById("logs-tbody");
    if (!tbody) return;
    tbody.innerHTML = "";

    if (inventoryLogs.length === 0) {
        tbody.innerHTML = `<tr><td colspan="7" style="text-align:center; padding: 24px; color:#64748b;">Chưa có lịch sử biến động kho.</td></tr>`;
        return;
    }

    inventoryLogs.forEach(log => {
        const dt = new Date(log.createdAt);
        const timeStr = isNaN(dt.getTime()) ? "-" : dt.toLocaleString("vi-VN");
        const qtyPrefix = log.quantityChange > 0 ? `+${log.quantityChange}` : `${log.quantityChange}`;
        const qtyColor = log.quantityChange >= 0 ? "#059669" : "#dc2626";

        let typeLabel = log.actionType;
        let cls = "log-update";
        if (log.actionType === "IMPORT") { typeLabel = "Nhập kho"; cls = "log-import"; }
        else if (log.actionType === "EXPORT") { typeLabel = "Xuất kho"; cls = "log-export"; }
        else if (log.actionType === "CREATE") { typeLabel = "Thêm mới"; cls = "log-create"; }
        else if (log.actionType === "UPDATE") { typeLabel = "Cập nhật"; cls = "log-update"; }
        else if (log.actionType === "DELETE") { typeLabel = "Xóa SP"; cls = "log-delete"; }

        const tr = document.createElement("tr");
        tr.innerHTML = `
            <td style="font-size:12.5px; color:#64748b;">${timeStr}</td>
            <td><span class="id-pill">${log.productId || "-"}</span></td>
            <td><strong>${log.productName}</strong></td>
            <td><span class="log-badge ${cls}">${typeLabel}</span></td>
            <td><strong style="color:${qtyColor};">${qtyPrefix}</strong></td>
            <td>${log.userName}</td>
            <td>${log.note || ""}</td>
        `;
        tbody.appendChild(tr);
    });
}

function showToast(message) {
    const toastBox = document.getElementById("toast-box");
    const toast = document.createElement("div");
    toast.className = "toast";
    toast.innerText = message;
    toastBox.appendChild(toast);
    setTimeout(() => toast.remove(), 3200);
}

async function resetSampleData() {
    if (currentRole !== "ADMIN") {
        showToast("Chỉ Quản lý mới có quyền khôi phục dữ liệu mẫu.");
        return;
    }
    if (confirm("Bạn muốn khôi phục lại 8 sản phẩm mẫu ban đầu?")) {
        if (isUsingBackend) {
            const res = await fetch(`${API_URL}/reset`, {
                method: "POST",
                headers: getAuthHeaders(false)
            });
            if (res.status === 401) { logoutUser(); return; }
            await loadAllData();
        } else {
            products = [...sampleProducts];
            localStorage.setItem(STORAGE_PRODUCTS_KEY, JSON.stringify(products));
            renderProducts();
        }
        resetForm();
        showToast("Đã khôi phục dữ liệu mẫu.");
    }
}

function exportToCSV() {
    if (products.length === 0) {
        alert("Kho hàng đang trống, không có dữ liệu để xuất.");
        return;
    }
    let csvContent = "\uFEFFMã SP,Tên sản phẩm,Danh mục,Giá bán (VNĐ),Tồn kho,Thành tiền (VNĐ)\n";
    products.forEach(item => {
        const total = item.price * item.quantity;
        csvContent += `${item.id},"${item.name}",${item.category},${item.price},${item.quantity},${total}\n`;
    });
    const blob = new Blob([csvContent], { type: "text/csv;charset=utf-8;" });
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.setAttribute("href", url);
    link.setAttribute("download", "BaoCao_KhoHang_DukeShop.csv");
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    showToast("Đã tải xuống báo cáo Excel (CSV).");
}
