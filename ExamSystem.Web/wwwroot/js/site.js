// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function confirmLogout() {
    Swal.fire({
        title: 'Đăng xuất?',
        text: "Bạn có chắc chắn muốn thoát phiên làm việc?",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Đăng xuất',
        cancelButtonText: 'Ở lại'
    }).then((result) => {
        if (result.isConfirmed) {
            // Nếu người dùng chọn Đăng xuất thì mới gửi form
            document.getElementById('logoutForm').submit();
        }
    })
}

// Đợi HTML tải xong
document.addEventListener("DOMContentLoaded", function () {
    // Tìm tất cả các alert có class 'auto-hide-alert'
    var alerts = document.querySelectorAll('.auto-hide-alert');

    alerts.forEach(function (alert) {
        // Thiết lập thời gian chờ 3000ms (3 giây)
        setTimeout(function () {
            // Kiểm tra xem trang có đang dùng Bootstrap 5 không để tắt mượt mà
            if (typeof bootstrap !== 'undefined') {
                var bsAlert = new bootstrap.Alert(alert);
                bsAlert.close();
            } else {
                // Nếu không dùng Bootstrap, ẩn bằng CSS thông thường
                alert.style.transition = "opacity 0.5s ease";
                alert.style.opacity = "0";
                setTimeout(() => alert.remove(), 500);
            }
        }, 3000);
    });
});

window.addEventListener('DOMContentLoaded', event => {
    const sidebarToggle = document.body.querySelector('#sidebarToggle');
    if (sidebarToggle) {
        // Xử lý click nút toggle
        sidebarToggle.addEventListener('click', event => {
            event.preventDefault();
            document.body.classList.toggle('sb-sidenav-toggled');

            // Lưu trạng thái vào localStorage để nhớ khi F5 (chỉ trên desktop)
            if (window.innerWidth >= 768) {
                localStorage.setItem('sb|sidebar-toggle', document.body.classList.contains('sb-sidenav-toggled'));
            }
        });
    }

    // Khôi phục trạng thái sidebar trên desktop
    if (window.innerWidth >= 768) {
        const isToggled = localStorage.getItem('sb|sidebar-toggle') === 'true';
        if (isToggled) {
            document.body.classList.add('sb-sidenav-toggled');
        }
    }
});

// Đợi HTML tải xong
document.addEventListener("DOMContentLoaded", function () {
    // Tìm tất cả các alert có class 'auto-hide-alert'
    var alerts = document.querySelectorAll('.auto-hide-alert');

    alerts.forEach(function (alert) {
        // Thiết lập thời gian chờ 3000ms (3 giây)
        setTimeout(function () {
            // Kiểm tra xem trang có đang dùng Bootstrap 5 không để tắt mượt mà
            if (typeof bootstrap !== 'undefined') {
                var bsAlert = new bootstrap.Alert(alert);
                bsAlert.close();
            } else {
                // Nếu không dùng Bootstrap, ẩn bằng CSS thông thường
                alert.style.transition = "opacity 0.5s ease";
                alert.style.opacity = "0";
                setTimeout(() => alert.remove(), 500);
            }
        }, 3000);
    });
});

function showNotification(message, isSuccess) {
    // 1. Tạo một thẻ div chứa thông báo
    const alertDiv = document.createElement('div');

    // 2. Xác định màu và icon dựa vào trạng thái success hay error
    const alertClass = isSuccess ? 'alert-success' : 'alert-danger';
    const iconClass = isSuccess ? 'bi-check-circle-fill' : 'bi-exclamation-triangle-fill';

    // 3. Gắn các class Bootstrap và CSS (cách top 60px, luôn nổi lên trên)
    alertDiv.className = `alert ${alertClass} alert-dismissible fade show position-fixed end-0 m-3 shadow`;
    alertDiv.style.top = '60px';
    alertDiv.style.zIndex = '1050';
    alertDiv.setAttribute('role', 'alert');

    // 4. Đổ nội dung HTML vào
    alertDiv.innerHTML = `
        <i class="bi ${iconClass} me-2"></i> ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;

    // 5. Gắn thông báo vào giao diện (thẻ body)
    document.body.appendChild(alertDiv);

    // 6. Đặt giờ tự động ẩn sau 3 giây (3000 ms)
    setTimeout(() => {
        if (typeof bootstrap !== 'undefined') {
            // Nếu có dùng Bootstrap 5
            const bsAlert = new bootstrap.Alert(alertDiv);
            bsAlert.close();
        } else {
            // Nếu không dùng thư viện Bootstrap JS, tự làm mờ bằng CSS
            alertDiv.style.transition = "opacity 0.5s ease";
            alertDiv.style.opacity = "0";
            setTimeout(() => alertDiv.remove(), 500);
        }
    }, 3000);
}

// --- LOGIC ĐỔI THEME SÁNG / TỐI ---
//document.addEventListener('DOMContentLoaded', () => {
//    const themeToggleBtn = document.getElementById('theme-toggle');
//    // Lấy theme đã lưu hoặc mặc định là light
//    const currentTheme = localStorage.getItem('theme') || 'light';

//    // Áp dụng theme ngay khi load
//    document.documentElement.setAttribute('data-theme', currentTheme);
//    document.documentElement.setAttribute('data-bs-theme', currentTheme);

//    if (typeof updateThemeIcon === 'function') {
//        updateThemeIcon(currentTheme);
//    }

//    // Bắt sự kiện click nút đổi màu
//    if (themeToggleBtn) {
//        themeToggleBtn.addEventListener('click', () => {
//            let theme = document.documentElement.getAttribute('data-theme');
//            let newTheme = theme === 'dark' ? 'light' : 'dark';

//            document.documentElement.setAttribute('data-theme', newTheme);
//            document.documentElement.setAttribute('data-bs-theme', newTheme);

//            localStorage.setItem('theme', newTheme);

//            if (typeof updateThemeIcon === 'function') {
//                updateThemeIcon(newTheme);
//            }
//        });
//    }
//});

//function updateThemeIcon(theme) {
//    const icon = document.querySelector('#theme-toggle i');
//    if (!icon) return;

//    if (theme === 'dark') {
//        icon.className = 'bi bi-moon-stars-fill text-warning';
//    } else {
//        icon.className = 'bi bi-sun-fill text-warning';
//    }
//}

function toggleTheme() {
    const checkbox = document.getElementById('theme-toggle');
    const text = document.getElementById('theme-text');
    const htmlElement = document.documentElement;

    if (checkbox.checked) {
        // Chế độ Tối (Bật đèn)
        htmlElement.setAttribute('data-bs-theme', 'dark');
        text.innerText = "Dark Mode";
        localStorage.setItem('theme', 'dark');
    } else {
        // Chế độ Sáng (Tắt đèn)
        htmlElement.setAttribute('data-bs-theme', 'light');
        text.innerText = "Light Mode";
        localStorage.setItem('theme', 'light');
    }
}

document.addEventListener('DOMContentLoaded', () => {
    const themeToggle = document.getElementById('theme-toggle');
    const currentTheme = localStorage.getItem('theme') || 'light';

    // KHÔNG CẦN gọi applyTheme ở đây nữa vì <head> đã làm rồi

    if (themeToggle) {
        // Đồng bộ trạng thái checkbox
        themeToggle.checked = (currentTheme === 'dark');
        updateBulbUI(currentTheme);

        themeToggle.addEventListener('change', () => {
            const newTheme = themeToggle.checked ? 'dark' : 'light';

            // Áp dụng và lưu
            applyTheme(newTheme);
            localStorage.setItem('theme', newTheme);
            updateBulbUI(newTheme);
        });
    }
});

function applyTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    document.documentElement.setAttribute('data-bs-theme', theme);
}

// Hàm cập nhật giao diện riêng cho nút Bóng đèn
function updateBulbUI(theme) {
    const themeText = document.getElementById('theme-text');
    if (themeText) {
        themeText.innerText = (theme === 'dark' ? 'TỐI' : 'SÁNG');
    }
}

// Hàm dùng chung cho tất cả các ô mật khẩu
function togglePassword(inputId, iconId) {
    const passInput = document.getElementById(inputId);
    const toggleIcon = document.getElementById(iconId);

    if (!passInput || !toggleIcon) return;

    if (passInput.type === "password") {
        passInput.type = "text";
        toggleIcon.classList.replace("bi-eye-slash", "bi-eye");
        // Nếu bạn dùng bản fill (có màu đậm) thì dùng dòng dưới:
        // toggleIcon.classList.replace("bi-eye-slash-fill", "bi-eye-fill");
    } else {
        passInput.type = "password";        
        toggleIcon.classList.replace("bi-eye", "bi-eye-slash");
    }
}

document.addEventListener("DOMContentLoaded", function () {
    const hero = document.querySelector('.hero-section');

    if (hero) {
        hero.addEventListener('mousemove', function (e) {
            // Lấy kích thước và vị trí của khung hero-section
            const rect = hero.getBoundingClientRect();

            // Tính tọa độ chuột hiện tại bên trong khung
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;

            // 1. Truyền tọa độ cho vầng sáng dạ quang
            hero.style.setProperty('--mouse-x', `${x}px`);
            hero.style.setProperty('--mouse-y', `${y}px`);

            // 2. Tính toán độ lệch cho hiệu ứng "né chuột"
            const centerX = rect.width / 2;
            const centerY = rect.height / 2;

            // Hệ số 0.05 quyết định mức độ né xa hay gần (chị có thể tăng lên 0.1 nếu muốn nó né mạnh hơn)
            const shiftX = (centerX - x) * 0.5;
            const shiftY = (centerY - y) * 0.5;

            hero.style.setProperty('--shift-x', shiftX);
            hero.style.setProperty('--shift-y', shiftY);
        });

        // Khi chuột rời khỏi khu vực, đưa các chấm bi về lại vị trí trung tâm
        hero.addEventListener('mouseleave', function () {
            hero.style.setProperty('--shift-x', 0);
            hero.style.setProperty('--shift-y', 0);
        });
    }
});

function openCloneModal(id, currentTitle) {
    document.getElementById('originalId').value = id;
    document.getElementById('newTitleInput').value = currentTitle + " (Bản sao)";
    var myModal = new bootstrap.Modal(document.getElementById('cloneModal'));
    myModal.show();
}

// Khởi tạo mức zoom mặc định
let currentScale = parseFloat(localStorage.getItem('dashboard-scale')) || 1.0;

function scaleContent(type) {
    const content = document.getElementById('zoomable-content');
    const display = document.getElementById('scale-level');

    if (!content) return;

    // Tính toán mức zoom mới
    if (type === 'in' && currentScale < 1.5) {
        currentScale += 0.1;
    } else if (type === 'out' && currentScale > 0.5) {
        currentScale -= 0.1;
    } else if (type === 'reset') {
        currentScale = 1.0;
    }

    // Làm tròn số để tránh lỗi phẩy thập phân (vd: 0.900000001)
    currentScale = Math.round(currentScale * 10) / 10;

    // 1. Cập nhật con số hiển thị trên giao diện
    if (display) display.innerText = Math.round(currentScale * 100) + '%';

    // 2. Thực hiện thu phóng (Ưu tiên thuộc tính zoom để không lệch layout)
    if (typeof content.style.zoom !== "undefined") {
        content.style.zoom = currentScale;
    } else {
        // Fallback cho Firefox: Dùng transform và ép lề trái
        content.style.transform = `scale(${currentScale})`;
        content.style.transformOrigin = 'top left';
        content.style.width = (100 / currentScale) + '%';
    }

    // 3. Lưu vào máy để khi load trang khác không bị mất
    localStorage.setItem('dashboard-scale', currentScale);
}

// Khi vừa load trang, áp dụng ngay mức zoom đã lưu
document.addEventListener("DOMContentLoaded", function () {
    scaleContent('apply-saved');
});