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