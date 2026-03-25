function confirmDelete(id) {
    Swal.fire({
        title: 'Xóa kết quả?',
        text: "Dữ liệu bài làm này sẽ bị xóa vĩnh viễn!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Xóa ngay',
        cancelButtonText: 'Hủy'
    }).then((result) => {
        if (result.isConfirmed) {

            // Dùng Fetch API gửi request POST
            fetch(`/Admin/ExamResults/Delete/${id}`, {
                method: 'POST'
            })
                .then(response => {
                    if (response.ok) {
                        Swal.fire(
                            'Đã xóa!',
                            'Dữ liệu bài làm đã được xóa.',
                            'success'
                        ).then(() => {
                            // Tải lại trang để cập nhật danh sách
                            window.location.reload();
                        });
                    } else {
                        Swal.fire('Lỗi!', 'Có lỗi xảy ra khi xóa.', 'error');
                    }
                })
                .catch(error => {
                    Swal.fire('Lỗi!', 'Không thể kết nối tới máy chủ.', 'error');
                });

        }
    })
}

function copyToClipboard(text) {
    // Tạo đường dẫn đầy đủ
    var fullUrl = window.location.origin + text;
    navigator.clipboard.writeText(fullUrl).then(function () {
        // Dùng SweetAlert2 nếu có, không thì alert thường
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                icon: 'success',
                title: 'Đã sao chép!',
                text: fullUrl,
                timer: 1500,
                showConfirmButton: false
            });
        } else {
            alert('Đã sao chép link: ' + fullUrl);
        }
    }, function (err) {
        console.error('Lỗi sao chép: ', err);
    });
}

//function confirmDelete(userId, userName) {
//    Swal.fire({
//        title: 'Xóa tài khoản?',
//        html: `Bạn có chắc chắn muốn xóa người dùng <b>${userName}</b>?<br><span class="text-danger small">Hành động này không thể hoàn tác!</span>`,
//        icon: 'warning',
//        showCancelButton: true,
//        confirmButtonColor: '#d33',
//        cancelButtonColor: '#6c757d',
//        confirmButtonText: 'Đồng ý xóa',
//        cancelButtonText: 'Hủy bỏ',
//        customClass: {
//            confirmButton: 'rounded-pill px-4 fw-bold',
//            cancelButton: 'rounded-pill px-4 fw-bold',
//            popup: 'rounded-4'
//        }
//    }).then((result) => {
//        if (result.isConfirmed) {
//            document.getElementById('delete-form-' + userId).submit();
//        }
//    });
//}

function confirmDelete(id, title) {
    Swal.fire({
        title: 'Xác nhận xóa?',
        html: `Bạn có chắc chắn muốn xóa dữ liệu <b>${title}</b>?<br><span class="text-danger small">Hành động này không thể hoàn tác!</span>`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Vâng, Xóa ngay',
        cancelButtonText: 'Hủy bỏ',
        customClass: {
            confirmButton: 'rounded-pill px-4 fw-bold',
            cancelButton: 'rounded-pill px-4 fw-bold',
            popup: 'rounded-4'
        }
    }).then((result) => {
        if (result.isConfirmed) {
            // Hiển thị loading cho đẹp trong lúc chờ server xử lý
            Swal.fire({
                title: 'Đang xóa...',
                allowOutsideClick: false,
                didOpen: () => { Swal.showLoading(); }
            });
            // Tìm cái form ẩn tương ứng với ID và bấm nút Submit
            document.getElementById('delete-form-' + id).submit();
        }
    });
}

// --- LOGIC CHECKBOX VÀ XÓA HÀNG LOẠT MỚI ---
document.addEventListener('DOMContentLoaded', function () {
    const checkAll_UsersBtn = document.getElementById('checkAll_Users');
    const userCheckboxes = document.querySelectorAll('.user-checkbox');
    const bulkContainer = document.getElementById('bulkActionContainer');
    const selectedCountDisplay = document.getElementById('selectedCount');

    function updateBulkActionUI() {
        const checkedCount = document.querySelectorAll('.user-checkbox:checked').length;
        if (checkedCount > 0) {
            selectedCountDisplay.innerText = checkedCount;
            bulkContainer.classList.remove('d-none');
        } else {
            bulkContainer.classList.add('d-none');
        }

        // Đồng bộ nút checkAll_Users
        checkAll_UsersBtn.checked = (checkedCount === userCheckboxes.length && userCheckboxes.length > 0);
    }

    // Bắt sự kiện Check All
    if (checkAll_UsersBtn) {
        checkAll_UsersBtn.addEventListener('change', function () {
            userCheckboxes.forEach(cb => cb.checked = this.checked);
            updateBulkActionUI();
        });
    }

    // Bắt sự kiện Check từng dòng
    userCheckboxes.forEach(cb => {
        cb.addEventListener('change', updateBulkActionUI);
    });
});


// Hàm gọi API xóa nhiều
function confirmDeleteMultiple() {
    const checkedBoxes = document.querySelectorAll('.user-checkbox:checked');
    const idsToDelete = Array.from(checkedBoxes).map(cb => cb.value);

    if (idsToDelete.length === 0) return;

    Swal.fire({
        title: 'CẢNH BÁO!',
        html: `Bạn đang chuẩn bị xóa <b>${idsToDelete.length}</b> tài khoản.<br/><span class="text-danger">Toàn bộ dữ liệu liên quan sẽ bị xóa vĩnh viễn.</span> Bạn có chắc không?`,
        icon: 'error',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'XÓA TẤT CẢ',
        cancelButtonText: 'Hủy bỏ',
        customClass: { popup: 'rounded-4' }
    }).then((result) => {
        if (result.isConfirmed) {
            // Hiển thị loading
            Swal.fire({
                title: 'Đang xóa...',
                allowOutsideClick: false,
                didOpen: () => { Swal.showLoading(); }
            });

            // Gọi Fetch API
            fetch('/Admin/Users/DeleteMultiple', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(idsToDelete)
            })
                .then(res => res.json())
                .then(data => {
                    if (data.success) {
                        window.location.reload(); // Reload để hiển thị TempData SuccessMessage
                    } else {
                        Swal.fire('Lỗi', data.message, 'error');
                    }
                })
                .catch(err => {
                    Swal.fire('Lỗi', 'Không thể kết nối đến máy chủ', 'error');
                });
        }
    });
}

function confirmDeleteMultipleLisening() {
    const checkedBoxes = document.querySelectorAll('.user-checkbox:checked');
    const idsToDelete = Array.from(checkedBoxes).map(cb => cb.value);

    if (idsToDelete.length === 0) return;

    Swal.fire({
        title: 'CẢNH BÁO!',
        html: `Bạn đang chuẩn bị xóa <b>${idsToDelete.length}</b> bài nghe.<br/><span class="text-danger">Toàn bộ dữ liệu liên quan sẽ bị xóa vĩnh viễn.</span> Bạn có chắc không?`,
        icon: 'error',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'XÓA TẤT CẢ',
        cancelButtonText: 'Hủy bỏ',
        customClass: { popup: 'rounded-4' }
    }).then((result) => {
        if (result.isConfirmed) {
            // Hiển thị loading
            Swal.fire({
                title: 'Đang xóa...',
                allowOutsideClick: false,
                didOpen: () => { Swal.showLoading(); }
            });

            // Gọi Fetch API
            fetch('/Admin/ListeningResources/DeleteMultiple', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(idsToDelete)
            })
                .then(res => res.json())
                .then(data => {
                    if (data.success) {
                        window.location.reload(); // Reload để hiển thị TempData SuccessMessage
                    } else {
                        Swal.fire('Lỗi', data.message, 'error');
                    }
                })
                .catch(err => {
                    Swal.fire('Lỗi', 'Không thể kết nối đến máy chủ', 'error');
                });
        }
    });
}

function confirmDeleteMultipleReading() {
    const checkedBoxes = document.querySelectorAll('.user-checkbox:checked');
    const idsToDelete = Array.from(checkedBoxes).map(cb => cb.value);

    if (idsToDelete.length === 0) return;

    Swal.fire({
        title: 'CẢNH BÁO!',
        html: `Bạn đang chuẩn bị xóa <b>${idsToDelete.length}</b> bài đọc.<br/><span class="text-danger">Toàn bộ dữ liệu liên quan sẽ bị xóa vĩnh viễn.</span> Bạn có chắc không?`,
        icon: 'error',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'XÓA TẤT CẢ',
        cancelButtonText: 'Hủy bỏ',
        customClass: { popup: 'rounded-4' }
    }).then((result) => {
        if (result.isConfirmed) {
            // Hiển thị loading
            Swal.fire({
                title: 'Đang xóa...',
                allowOutsideClick: false,
                didOpen: () => { Swal.showLoading(); }
            });

            // Gọi Fetch API
            fetch('/Admin/ReadingPassages/DeleteMultiple', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify(idsToDelete)
            })
                .then(res => res.json())
                .then(data => {
                    if (data.success) {
                        window.location.reload(); // Reload để hiển thị TempData SuccessMessage
                    } else {
                        Swal.fire('Lỗi', data.message, 'error');
                    }
                })
                .catch(err => {
                    Swal.fire('Lỗi', 'Không thể kết nối đến máy chủ', 'error');
                });
        }
    });
}