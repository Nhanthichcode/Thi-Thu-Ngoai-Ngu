// =========================================================================
// 1. TRANG QUẢN LÝ CÂU HỎI (INDEX.CSHTML)
// =========================================================================
document.addEventListener("DOMContentLoaded", function () {
    // Tự động submit khi chọn Level
    const levelSelect = document.getElementById('levelSelect');
    if (levelSelect) {
        levelSelect.addEventListener('change', function () {
            document.getElementById('filterForm').submit();
        });
    }

    // Đảo chiều mũi tên khi click mở từng nhóm
    document.querySelectorAll('[data-bs-toggle="collapse"]').forEach(el => {
        el.addEventListener('click', function () {
            let icon = this.querySelector('.collapse-icon');
            if (icon) {
                if (icon.classList.contains('bi-chevron-down')) {
                    icon.classList.replace('bi-chevron-down', 'bi-chevron-up');
                } else {
                    icon.classList.replace('bi-chevron-up', 'bi-chevron-down');
                }
            }
        });
    });
});

function toggleAll(action) {
    var collapseElementList = [].slice.call(document.querySelectorAll('.group-collapse-content'));
    collapseElementList.forEach(function (collapseEl) {
        var bsCollapse = bootstrap.Collapse.getInstance(collapseEl);
        if (!bsCollapse) {
            bsCollapse = new bootstrap.Collapse(collapseEl, { toggle: false });
        }

        let header = collapseEl.previousElementSibling;
        let icon = header ? header.querySelector('.collapse-icon') : null;

        if (action === 'show') {
            bsCollapse.show();
            if (icon) {
                icon.classList.replace('bi-chevron-right', 'bi-chevron-down');
                icon.classList.replace('bi-chevron-up', 'bi-chevron-down');
            }
        } else {
            bsCollapse.hide();
            if (icon) {
                icon.classList.replace('bi-chevron-down', 'bi-chevron-up');
            }
        }
    });

    // --- LOGIC MỚI: TRÁO ĐỔI NÚT ẨN/HIỆN ---
    const btnExpand = document.getElementById('btnExpandAll');
    const btnCollapse = document.getElementById('btnCollapseAll');

    if (action === 'show') {
        // Nếu vừa bấm "Mở rộng" -> Ẩn nút "Mở rộng", Hiện nút "Thu gọn"
        if (btnExpand) btnExpand.classList.add('d-none');
        if (btnCollapse) btnCollapse.classList.remove('d-none');
    } else {
        // Nếu vừa bấm "Thu gọn" -> Ẩn nút "Thu gọn", Hiện nút "Mở rộng"
        if (btnExpand) btnExpand.classList.remove('d-none');
        if (btnCollapse) btnCollapse.classList.add('d-none');
    }
}

function confirmDelete(e, url, title) {
    e.preventDefault();
    Swal.fire({
        title: 'Xóa câu hỏi?',
        html: `Bạn có chắc muốn xóa <b>${title || 'nhóm câu hỏi này'}</b>?<br><span class="text-danger small">Toàn bộ câu hỏi bên trong sẽ bị xóa vĩnh viễn!</span>`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Đồng ý xóa',
        cancelButtonText: 'Hủy bỏ',
        customClass: {
            confirmButton: 'rounded-pill px-4',
            cancelButton: 'rounded-pill px-4',
            popup: 'rounded-4'
        }
    }).then((result) => {
        if (result.isConfirmed) {
            window.location.href = url;
        }
    });
    return false;
}

// =========================================================================
// 2. TRANG SOẠN THẢO / CHỈNH SỬA (CREATE.CSHTML & EDIT.CSHTML)
// =========================================================================
$(document).ready(function () {
    const skillSelectCreate = document.getElementById("SkillSelect");
    const skillSelectEdit = document.getElementById("SkillTypeHidden");

    // Chỉ chạy logic nếu đang ở trang Create hoặc Edit
    if (skillSelectCreate || skillSelectEdit) {

        function updateQuestionUI() {
            var skill = skillSelectCreate ? $(skillSelectCreate).val() : $(skillSelectEdit).val();

            // Ẩn tất cả section tài nguyên
            $(".form-section").hide();
            $("#block-reading").hide();
            $("#block-listening").hide();
            $("#block-image").hide();

            // LOGIC CHO TRANG EDIT
            if (skillSelectEdit) {
                $("#block-answers").show();
                $("#block-no-answers").hide();
                if (skill == "1") $("#block-listening").show();
                else if (skill == "2") $("#block-reading").show();
                else if (skill == "3") { $("#block-answers").hide(); $("#block-no-answers").show(); }
                else if (skill == "4") { $("#block-image").show(); $("#block-answers").hide(); $("#block-no-answers").show(); }
            }
            // LOGIC CHO TRANG CREATE
            else if (skillSelectCreate) {
                if (skill == "1" || skill == "2" || skill == "4") {
                    $("#leftPane").show();
                    $("#rightPane").removeClass("col-lg-12").addClass("col-lg-7");
                    if (skill == "1") $("#section-listening").fadeIn(300);
                    if (skill == "2") $("#section-reading").fadeIn(300);
                    if (skill == "4") $("#section-image").fadeIn(300);
                } else {
                    $("#leftPane").hide();
                    $("#rightPane").removeClass("col-lg-7").addClass("col-lg-12");
                }

                if (skill == "3" || skill == "4") {
                    $(".answer-block").addClass("d-none");
                    $(".no-answer-block").removeClass("d-none");
                } else {
                    $(".answer-block").removeClass("d-none");
                    $(".no-answer-block").addClass("d-none");
                }
                if (skill == "4") $(".explain-block").addClass("d-none");
                else $(".explain-block").removeClass("d-none");
            }
        }

        if (skillSelectCreate) {
            $(skillSelectCreate).change(function () {
                updateQuestionUI();
            });
        }

        // Khởi chạy giao diện lần đầu
        updateQuestionUI();

        // Tự động thêm câu hỏi đầu tiên ở trang Create
        if (document.getElementById('q-template')) {
            addQuestion();
        }

        // AJAX tải nội dung tài nguyên
        var apiUrl = '/Admin/Questions/GetResourceDetails';
        $("#SelectReadingId").change(function () {
            var id = $(this).val();
            if (id) {
                $.get(apiUrl + "?type=reading&id=" + id, function (d) {
                    if (d.success) {
                        $("#ReadingPreview").html(d.content);
                        $("#ReadingPreviewContainer").hide().removeClass('d-none').slideDown(300);
                    }
                }).fail(function () { alert("Lỗi khi tải dữ liệu bài đọc!"); });
            } else {
                $("#ReadingPreviewContainer").slideUp(200, function () { $(this).addClass('d-none'); });
            }
        });

        $("#SelectListeningId").change(function () {
            var id = $(this).val();
            if (id) {
                $.get(apiUrl + "?type=listening&id=" + id, function (d) {
                    if (d.success) {
                        $("#AudioPlayer").attr('src', d.audioUrl);
                        $("#TranscriptPreview").html(d.transcript ? d.transcript : "<i class='text-muted'>Chưa có bản dịch.</i>");
                        $("#ListeningPreviewContainer").hide().removeClass('d-none').slideDown(300);
                    }
                }).fail(function () { alert("Lỗi khi tải dữ liệu bài nghe!"); });
            } else {
                $("#ListeningPreviewContainer").slideUp(200, function () { $(this).addClass('d-none'); $("#AudioPlayer").attr('src', ''); });
            }
        });

        // Preview ảnh
        $("#imageInput").change(function () {
            var file = this.files[0];
            if (file) {
                var reader = new FileReader();
                reader.onload = function (e) {
                    $("#img-preview").attr("src", e.target.result);
                    $("#preview-container").removeClass("d-none");
                }
                reader.readAsDataURL(file);
            } else {
                $("#preview-container").addClass("d-none");
            }
        });
    }
});

// Các hàm cho trang Create
var qCounter = 0;
function addQuestion() {
    var template = document.getElementById('q-template');
    if (!template) return;
    var html = template.innerHTML.replace(/{INDEX}/g, qCounter);
    var $newItem = $(html).hide();
    $("#questions-container").append($newItem);

    // Kích hoạt event cập nhật UI để ẩn hiện đúng các khối
    const skillSelectCreate = document.getElementById("SkillSelect");
    if (skillSelectCreate) $(skillSelectCreate).trigger('change');

    $newItem.fadeIn(300);
    qCounter++;
    updateNumbers();
}

function removeQ(btn) {
    if (confirm('Xóa câu hỏi này?')) {
        $(btn).closest(".q-item").fadeOut(300, function () {
            $(this).remove();
            updateNumbers();
        });
    }
}

function updateNumbers() {
    var total = 0;
    $(".q-item").each(function (i) {
        $(this).find(".q-num").text(i + 1);
        total++;
    });
    $("#totalQuestionsBadge").text(total + " câu");
}

window.previewImage = function (input) {
    if (input.files && input.files[0]) {
        var reader = new FileReader();
        reader.onload = function (e) { $('#imgPreview').attr('src', e.target.result).removeClass('d-none'); }
        reader.readAsDataURL(input.files[0]);
    }
}

// Các hàm cho trang Edit
function deleteCurrentMedia() {
    if (confirm("Bạn có chắc chắn muốn xóa ảnh này? Thay đổi sẽ được lưu sau khi cập nhật.")) {
        $("#deleteImageFlag").val("true");
        $("#MediaUrl").val("");
        $("#current-image-wrapper").remove();
        $("#current-image-container").append('<div class="text-muted small py-4 border rounded bg-light" id="no-image-placeholder">Đã xóa ảnh</div>');
        $("#imageInput").val('');
        $("#preview-container").addClass("d-none");
    }
}

// =========================================================================
// 3. TRANG BATCH EDIT (SỬA HÀNG LOẠT)
// =========================================================================
var deletedIds = [];
function markDelete(btn, id) {
    if (confirm("Bạn muốn xóa câu hỏi này khỏi bài?")) {
        deletedIds.push(id);
        document.getElementById('DeletedIdsInput').value = deletedIds.join(',');
        $(btn).closest('.q-item').fadeOut(300, function () { $(this).remove(); });
    }
}

function addBlankQuestion() {
    var template = document.getElementById('new-q-template');
    if (!template) return;

    var batchCurrentIndex = document.querySelectorAll('#questions-container .q-item').length + deletedIds.length + 100; // Cộng dồn để né ID cũ
    var html = template.innerHTML.replace(/{INDEX}/g, batchCurrentIndex);
    document.getElementById('questions-container').insertAdjacentHTML('beforeend', html);
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

    var questions = document.querySelectorAll('.q-item');
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
        document.getElementById('batchEditForm').submit();
    }
}

// =========================================================================
// 4. TRANG IMPORT EXCEL
// =========================================================================
function resetState() {
    const contentState = document.getElementById('contentState');
    if (contentState) {
        contentState.classList.add('d-none');
        document.getElementById('emptyState').classList.remove('d-none');

        var btnSave = document.getElementById('btnSave');
        if (btnSave) {
            btnSave.disabled = true;
            btnSave.className = "btn btn-secondary btn-lg fw-bold shadow-sm rounded-pill";
            btnSave.style.backgroundColor = "";
        }

        var badge = document.getElementById('statusBadge');
        if (badge) {
            badge.className = "badge bg-secondary rounded-pill px-3 py-2 fw-normal";
            badge.innerText = "Chờ xử lý";
        }
    }
}

function submitAjax(mode) {
    var fileInput = document.getElementById('fileInput');
    if (!fileInput || fileInput.files.length === 0) {
        Swal.fire('Chú ý', 'Vui lòng chọn file Excel trước!', 'warning');
        return;
    }

    if (mode === 'save' && !confirm("Bạn có chắc chắn muốn lưu dữ liệu này vào hệ thống?")) return;

    var formData = new FormData();
    formData.append("file", fileInput.files[0]);
    formData.append("mode", mode);

    var token = document.querySelector('input[name="__RequestVerificationToken"]');
    if (token) formData.append("__RequestVerificationToken", token.value);

    document.getElementById('loadingArea').classList.remove('d-none');
    document.getElementById('resultArea').classList.add('d-none');

    // Chỉnh URL Tĩnh (Cố định đường dẫn) thay cho @Url.Action để tránh lỗi 404
    fetch('/Admin/Questions/Import', { method: 'POST', body: formData })
        .then(r => r.json())
        .then(data => {
            document.getElementById('loadingArea').classList.add('d-none');
            document.getElementById('resultArea').classList.remove('d-none');
            renderResult(data, mode);
        })
        .catch(err => {
            Swal.fire('Lỗi kết nối', err.toString(), 'error');
            document.getElementById('loadingArea').classList.add('d-none');
            document.getElementById('resultArea').classList.remove('d-none');
        });
}

function renderResult(data, mode) {
    if (mode === 'save' && data.redirectUrl) {
        Swal.fire({
            icon: 'success',
            title: 'Nhập dữ liệu thành công!',
            text: data.message || 'Hệ thống đang chuyển hướng...',
            showConfirmButton: false,
            timer: 2000
        }).then(() => {
            window.location.href = data.redirectUrl;
        });
        return;
    }

    var isSuccess = data.isSuccess || data.IsSuccess || false;
    var message = data.message || data.Message || "";
    var errors = data.errors || data.Errors || [];
    var validCount = data.validCount || data.ValidCount || 0;
    var invalidCount = data.invalidCount || data.InvalidCount || 0;

    var btnSave = document.getElementById('btnSave');
    var badge = document.getElementById('statusBadge');

    document.getElementById('emptyState').classList.add('d-none');
    document.getElementById('contentState').classList.remove('d-none');
    document.getElementById('messageBox').classList.add('d-none');
    document.getElementById('checkSuccessArea').classList.add('d-none');
    document.getElementById('errorArea').classList.add('d-none');

    if (isSuccess && mode === 'check') {
        btnSave.disabled = false;
        btnSave.className = "btn text-white btn-lg fw-bold shadow-sm rounded-pill hover-up pulse-animation";
        btnSave.style.backgroundColor = "var(--bs-success, #10b981)";

        badge.className = "badge bg-success rounded-pill px-3 py-2 fw-normal shadow-sm";
        badge.innerText = "Hợp Lệ 100%";

        document.getElementById('checkSuccessArea').classList.remove('d-none');
        document.getElementById('successDetailText').innerText = message;
    }
    else {
        btnSave.disabled = true;
        btnSave.className = "btn btn-secondary btn-lg fw-bold shadow-sm rounded-pill";
        btnSave.style.backgroundColor = "";

        if (errors.length > 0) {
            badge.className = "badge bg-danger rounded-pill px-3 py-2 fw-normal shadow-sm";
            badge.innerText = "Phát hiện Lỗi";

            document.getElementById('errorArea').classList.remove('d-none');
            document.getElementById('validCount').innerText = validCount;
            document.getElementById('invalidCount').innerText = invalidCount;

            var msgBox = document.getElementById('messageBox');
            msgBox.className = "alert bg-danger bg-opacity-10 border-0 d-flex align-items-center rounded-3 shadow-sm";
            msgBox.innerHTML = `<i class="bi bi-x-circle-fill text-danger me-3 fs-4"></i><div class="text-danger fw-bold">${message}</div>`;
            msgBox.classList.remove('d-none');

            var tbody = document.getElementById('errorTableBody');
            tbody.innerHTML = "";
            errors.forEach(err => {
                var r = err.row || err.Row;
                var m = err.errorMessage || err.ErrorMessage;
                tbody.innerHTML += `<tr>
                            <td class="text-center fw-bold text-danger border-end bg-white">#${r}</td>
                            <td class="text-danger bg-white"><i class="bi bi-bug-fill me-2 opacity-50"></i>${m}</td>
                        </tr>`;
            });
        }
        else {
            badge.className = "badge bg-warning text-dark rounded-pill px-3 py-2 fw-normal shadow-sm";
            badge.innerText = "Cảnh báo";

            var msgBox = document.getElementById('messageBox');
            msgBox.className = "alert bg-warning bg-opacity-10 border-0 d-flex align-items-center rounded-3 shadow-sm";
            msgBox.innerHTML = `<i class="bi bi-exclamation-triangle-fill text-warning me-3 fs-4"></i><div class="text-dark fw-bold">${message}</div>`;
            msgBox.classList.remove('d-none');
        }
    }
}