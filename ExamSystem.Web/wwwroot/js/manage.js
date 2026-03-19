        var examId = @Model.Id;

        // --- 1. LOGIC GIỮ TAB SAU KHI RELOAD ---
        $(document).ready(function() {
            // Lấy tab đã lưu từ LocalStorage
            var activeTab = localStorage.getItem('activeExamTab');
        if (activeTab) {
                // Kích hoạt lại tab đó
                var tabTrigger = new bootstrap.Tab(document.querySelector('#' + activeTab + '-tab'));
        tabTrigger.show();
            }
        });

        function saveTab(tabName) {
            localStorage.setItem('activeExamTab', tabName);
        }

        // --- 2. CÁC HÀM MODAL & AJAX ---

        function openCreatePartModal(skillId) {
            $('#newPartName').val('');
        $('#newPartDesc').val('');
        $('#newPartSkill').val(skillId);
        $('#createPartModal').modal('show');
            setTimeout(() => $('#newPartName').focus(), 500);
        }

        function submitNewPart() {
            var name = $('#newPartName').val();
        var skill = $('#newPartSkill').val();
        var desc = $('#newPartDesc').val();

        if (!name) {alert("Nhập tên!"); return; }

        var btn = event.target;
        btn.disabled = true;
        btn.innerText = "Đang tạo...";

        $.post('/Admin/Exams/QuickAddPart', { // Nhớ thêm /Admin nếu có Area
            examId: examId, name: name, skill: skill, description: desc
            }, function(res) {
                if (res.success) window.location.reload();
        else {alert(res.message); btn.disabled = false; btn.innerText = "Lưu và Tạo"; }
            });
        }

        function openAddModal(partId, type, skillType) {
            $('#currentPartId').val(partId);
        $('#currentType').val(type);
        $('#addQuestionModal').modal('show');
        $('#modalLoading').removeClass('d-none');
        $('#modalContent').addClass('d-none');

        // Dùng Url.Action để đảm bảo đúng Area
        var url = '@Url.Action("GetAvailableQuestions", "Exams", new {area = "Admin"})';
        var fullUrl = url + '?examId=' + examId + '&type=' + type + '&skillType=' + skillType;
        console.log("Calling API:", fullUrl);

        $.get(fullUrl, function(data) {
            renderTable(data, type);
        $('#modalLoading').addClass('d-none');
        $('#modalContent').removeClass('d-none');
            }).fail(function(jqXHR, textStatus, errorThrown) {
            console.error("Lỗi API:", textStatus, errorThrown);
        alert("Lỗi tải dữ liệu. Mã lỗi: " + jqXHR.status);
        $('#modalLoading').addClass('d-none');
            });
        }

        function renderTable(data, type) {
            var html = '';
        if (!data || data.length === 0) {
            html = '<tr><td colspan="2" class="text-center text-muted py-4"><i class="bi bi-inbox fs-1 d-block mb-2"></i>Không tìm thấy dữ liệu phù hợp.</td></tr>';
            } else {
            data.forEach(item => {
                var btn = item.isSelected
                    ? '<button disabled class="btn btn-secondary btn-sm"><i class="bi bi-check2"></i> Đã chọn</button>'
                    : `<button class="btn btn-success btn-sm" onclick="addItem(${item.id})"><i class="bi bi-plus-lg"></i> Thêm</button>`;

                var content = type === 'Independent'
                    ? `<div>${item.content}</div><small class="text-muted">Level: ${item.level}</small>`
                    : `<div class="fw-bold">${item.title}</div><small class="text-muted">${item.questionCount} câu hỏi</small>`;

                html += `<tr><td>${content}</td><td class="text-center">${btn}</td></tr>`;
            });
            }
        $('#modalTableBody').html(html);

        if(type === 'Independent') $('#modalHint').text('Chọn câu hỏi lẻ để thêm vào phần thi này.');
        else $('#modalHint').text('Chọn bài đọc/nghe để thêm toàn bộ câu hỏi của bài đó.');
        }

        function addItem(itemId) {
            var partId = $('#currentPartId').val();
        var type = $('#currentType').val();
        var score = $('#defaultScore').val();

        // Sửa URL đúng chuẩn Area
        var url = type === 'Independent'
        ? '@Url.Action("AddSingleQuestion", "Exams", new {area = "Admin"})'
        : '@Url.Action("AddResourceGroup", "Exams", new {area = "Admin"})';

        var data = type === 'Independent'
        ? {examPartId: partId, questionId: itemId, score: score }
        : {examPartId: partId, resourceId: itemId, type: type, scorePerQuestion: score };

        postForm(url, data);
        }

        function postForm(path, params) {
            var form = document.createElement("form");
        form.setAttribute("method", "post");
        form.setAttribute("action", path);
        for(var key in params) {
                var hiddenField = document.createElement("input");
        hiddenField.setAttribute("type", "hidden");
        hiddenField.setAttribute("name", key);
        hiddenField.setAttribute("value", params[key]);
        form.appendChild(hiddenField);
            }
        document.body.appendChild(form);
        form.submit();
        }

        // --- 3. AJAX DELETE & CLEAR PART (KHÔNG RELOAD CẢ TRANG NẾU XÓA, RELOAD ĐÚNG TAB NẾU CLEAR) ---

        function deletePartAjax(partId) {
            if (!confirm('CẢNH BÁO: Bạn chắc chắn muốn xóa vĩnh viễn phần thi này?')) return;

        // Hiệu ứng mờ dần để người dùng biết đang xử lý
        const card = document.getElementById('part-card-' + partId);
        if(card) card.style.opacity = '0.5';

        // Gọi API Xóa (Đảm bảo Controller có hàm DeletePartAjax trả về JSON)
        $.post('/Admin/ExamParts/DeletePartAjax', {id: partId }, function(res) {
                if (res.success) {
            // Thành công: Xóa element khỏi giao diện ngay lập tức
            $(card).fadeOut(300, function () { $(this).remove(); });
                } else {
            alert(res.message);
        if(card) card.style.opacity = '1'; // Khôi phục nếu lỗi
                }
            }).fail(function() {
            alert("Lỗi kết nối server!");
        if(card) card.style.opacity = '1';
            });
        }

        // 2. Clear dữ liệu câu hỏi (Cần reload để hiện lại nút thêm câu hỏi)
        function clearPartData(partId) {
            if (!confirm('Bạn muốn gỡ bỏ tất cả câu hỏi trong phần này (Giữ lại cấu trúc Part)?')) return;

        $.post('/Admin/ExamParts/ClearPartQuestionsAjax', {id: partId }, function(res) {
                if (res.success) {
            // Vì cấu trúc nút bấm "Thêm câu hỏi" phụ thuộc vào việc Part có trống hay không
            // Nên ta reload trang. Nhờ logic saveTab ở trên, nó sẽ tự quay lại tab hiện tại.
            window.location.reload();
                } else {
            alert(res.message);
                }
            }).fail(function() {
            alert("Lỗi kết nối server!");
            });
        }