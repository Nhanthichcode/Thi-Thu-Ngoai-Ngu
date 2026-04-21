// Biến toàn cục lưu danh sách ID
var deletedIds = [];

// ==========================================
// 1. HÀM CẬP NHẬT GIAO DIỆN NÚT KHÔI PHỤC
// ==========================================
function updateRestoreUI() {
    var deletedCount = $('.deleted-item').length;
    var btnRestore = $('#btnRestore');

    // Đồng bộ mảng id vào thẻ input ẩn
    var deletedInput = document.getElementById('DeletedIdsInput');
    if (deletedInput) deletedInput.value = deletedIds.join(',');

    // Hiện nút Khôi phục nếu có câu bị xóa, ngược lại thì ẩn
    if (deletedCount > 0) {
        $('#restoreCount').text(deletedCount);
        btnRestore.removeClass('d-none');
    } else {
        btnRestore.addClass('d-none');
    }
}

// ==========================================
// 2. HÀM XÓA 1 CÂU HỎI
// ==========================================
function markDelete(btn, id) {
    Swal.fire({
        title: 'Xóa câu hỏi này?',
        text: "Bạn có thể khôi phục lại trước khi bấm Lưu.",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#dc3545',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Có, xóa nó',
        cancelButtonText: 'Hủy'
    }).then((result) => {
        if (result.isConfirmed) {
            if (id && id !== "0" && id !== 0) {
                if (!deletedIds.includes(id)) deletedIds.push(id);
            }

            // Thay vì .remove(), ta gán class và ẩn đi
            $(btn).closest('.q-item').addClass('deleted-item').fadeOut(300, function () {
                updateRestoreUI();
            });

            Swal.fire({ toast: true, position: 'top-end', icon: 'success', title: 'Đã tạm xóa.', showConfirmButton: false, timer: 1500 });
        }
    });
}

// ==========================================
// 3. HÀM XÓA TẤT CẢ CÂU HỎI
// ==========================================
function deleteAllQuestions() {
    // Chỉ lấy những câu CHƯA BỊ XÓA
    var activeQuestions = $('.q-item:not(.deleted-item)');
    if (activeQuestions.length === 0) {
        Swal.fire('Thông báo', 'Hiện không còn câu hỏi nào để xóa.', 'info');
        return;
    }

    Swal.fire({
        title: 'Xóa toàn bộ câu hỏi còn lại?',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#dc3545',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Có, Xóa tất cả',
        cancelButtonText: 'Hủy'
    }).then((result) => {
        if (result.isConfirmed) {
            activeQuestions.each(function () {
                var item = $(this);
                var id = item.attr('data-id');

                if (id && id !== "0" && id !== 0) {
                    if (!deletedIds.includes(id)) deletedIds.push(id);
                }

                item.addClass('deleted-item').fadeOut(300);
            });

            updateRestoreUI();
            Swal.fire({ toast: true, position: 'top-end', icon: 'success', title: 'Đã tạm xóa tất cả.', showConfirmButton: false, timer: 1500 });
        }
    });
}

// ==========================================
// 4. HÀM KHÔI PHỤC (UNDO) MỚI
// ==========================================
function restoreQuestions() {
    var hiddenItems = $('.deleted-item');
    if (hiddenItems.length === 0) return;

    // Hiển thị lại các thẻ và gỡ class
    hiddenItems.hide().removeClass('deleted-item').fadeIn(400);

    // Dọn dẹp sạch sẽ bộ nhớ chờ xóa
    deletedIds = [];
    updateRestoreUI();

    Swal.fire({
        toast: true,
        position: 'top-end',
        icon: 'success',
        title: 'Đã khôi phục thành công!',
        showConfirmButton: false,
        timer: 2000
    });
}

function showError(element, message) {
    var errorAlert = document.getElementById('error-alert');
    if (errorAlert) {
        document.getElementById('error-message').innerText = message;
        errorAlert.classList.remove('d-none');
    }

    if (element) {
        element.classList.add('is-invalid');
        element.focus();
        var headerOffset = 150;
        var elementPosition = element.getBoundingClientRect().top;
        var offsetPosition = elementPosition + window.pageYOffset - headerOffset;

        window.scrollTo({
            top: offsetPosition,
            behavior: "smooth"
        });

        element.oninput = function () {
            element.classList.remove('is-invalid');
            if (errorAlert) errorAlert.classList.add('d-none');
        };
    }
}

function addBlankQuestion() {
    var template = document.getElementById('new-q-template');
    if (!template) return;

    // Dùng timestamp (thời gian thực) để làm Index tạm thời, đảm bảo không bao giờ trùng lặp khi thêm liên tục
    var batchCurrentIndex = new Date().getTime();
    var html = template.innerHTML.replace(/{INDEX}/g, batchCurrentIndex);
    document.getElementById('questions-container').insertAdjacentHTML('beforeend', html);
}

function submitForm() {
    var errorAlert = document.getElementById('error-alert');
    if (errorAlert) errorAlert.classList.add('d-none');
    document.querySelectorAll('.is-invalid').forEach(el => el.classList.remove('is-invalid'));

    var title = document.getElementById('ResourceTitle');
    if (title && !title.value.trim()) {
        showError(title, "Vui lòng nhập Tiêu đề bài!");
        return;
    }

    var content = document.getElementById('ResourceContent');
    if (content && !content.value.trim()) {
        showError(content, "Vui lòng nhập Nội dung hoặc Transcript!");
        return;
    }

    // Lấy danh sách các câu hỏi CÒN LẠI trong vùng chứa
    var questions = document.querySelectorAll('#questions-container .q-item');
    if (questions.length === 0) {
        showError(null, "Bài này chưa có câu hỏi nào. Vui lòng thêm ít nhất 1 câu hỏi!");
        return;
    }

    var isValidQuestions = true;
    for (var i = 0; i < questions.length; i++) {
        if (questions[i].style.display === 'none') continue;
        var qContent = questions[i].querySelector('.q-content-input');
        if (qContent && !qContent.value.trim()) {
            showError(qContent, "Nội dung câu hỏi không được để trống!");
            isValidQuestions = false;
            break;
        }
    }

    if (isValidQuestions) {       
        questions.forEach(function (qItem, newIndex) {
            var inputs = qItem.querySelectorAll('input, select, textarea');
            inputs.forEach(function (input) {
                if (input.name && input.name.includes('Questions[')) {
                    // Thay thế chuỗi Questions[bất_kỳ_số_nào] thành Questions[newIndex]
                    input.name = input.name.replace(/Questions\[\w+\]/, 'Questions[' + newIndex + ']');
                }
            });
        });

        // Sau khi các name đã được xếp hàng ngay ngắn (0,1,2...), mới chính thức gửi Form
        document.getElementById('batchEditForm').submit();
    }
}

// Lôgic kéo thả thanh Resizer
const resizer = document.getElementById('resizer');
const leftPane = document.getElementById('left-pane');
const splitContainer = document.querySelector('.split-container');

let x = 0;
let leftWidth = 0;

const mouseDownHandler = function (e) {
    x = e.clientX;
    const leftPaneWidth = leftPane.getBoundingClientRect().width;
    leftWidth = (leftPaneWidth / splitContainer.getBoundingClientRect().width) * 100;

    document.addEventListener('mousemove', mouseMoveHandler);
    document.addEventListener('mouseup', mouseUpHandler);
    resizer.classList.add('resizing');
};

const mouseMoveHandler = function (e) {
    const dx = e.clientX - x;
    const containerWidth = splitContainer.getBoundingClientRect().width;
    const newLeftWidth = leftWidth + (dx / containerWidth) * 100;

    // Giới hạn cột trái từ 20% đến 80%
    if (newLeftWidth > 20 && newLeftWidth < 80) {
        leftPane.style.flex = `0 0 ${newLeftWidth}%`;
    }
};

const mouseUpHandler = function () {
    resizer.classList.remove('resizing');
    document.removeEventListener('mousemove', mouseMoveHandler);
    document.removeEventListener('mouseup', mouseUpHandler);
};


resizer.addEventListener('mousedown', mouseDownHandler);

// Logic Xóa Tất Cả Câu Hỏi
