//const questionSectionMap = @Html.Raw(System.Text.Json.JsonSerializer.Serialize(questionSectionMap));
let currentIndex = 0;

// --- 0. TOGGLE SIDEBAR (ẨN/HIỆN MENU) ---
function toggleSidebar() {
    const sidebar = document.getElementById('sidebarContainer');
    const icon = document.getElementById('sidebarToggleIcon');
    sidebar.classList.toggle('collapsed');

    if (sidebar.classList.contains('collapsed')) {
        icon.classList.replace('bi-chevron-right', 'bi-chevron-left');
    } else {
        icon.classList.replace('bi-chevron-left', 'bi-chevron-right');
    }
}

// Cấu hình ban đầu
window.onload = function () { goToSection(0); };

// --- 1. THEO DÕI VÀ ĐIỀU HƯỚNG BẢN ĐỒ CÂU HỎI ---
function jumpToQuestion(qIdx) {
    let secIdx = questionSectionMap[qIdx];
    if (currentIndex !== secIdx) {
        goToSection(secIdx);
    }
    setTimeout(() => {
        let qEl = document.getElementById('q-wrapper-' + qIdx);
        if (qEl) {
            qEl.scrollIntoView({ behavior: 'smooth', block: 'center' });
            highlightMapBtn(qIdx);
        }
    }, 100);
}

function highlightMapBtn(qIdx) {
    document.querySelectorAll('.map-btn').forEach(b => b.classList.remove('active-view'));
    let btn = document.getElementById('map-btn-' + qIdx);
    if (btn) btn.classList.add('active-view');
}

function markAnswered(qIdx, hasValue) {
    let btn = document.getElementById('map-btn-' + qIdx);
    if (btn) {
        if (hasValue) btn.classList.add('answered');
        else btn.classList.remove('answered');
    }
}

// Tự động phát hiện câu đang xem khi cuộn chuột
let observer = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
        if (entry.isIntersecting) {
            let qId = entry.target.id.split('-').pop();
            highlightMapBtn(qId);
        }
    });
}, { root: document.getElementById('mainContent'), threshold: 0.5 });

document.querySelectorAll('[id^="q-wrapper-"]').forEach(el => observer.observe(el));

// --- 2. ĐIỀU HƯỚNG TABS GOM NHÓM ---
function goToSection(index) {
    // Đổi content
    document.querySelectorAll('.exam-section').forEach(el => el.classList.remove('active'));
    document.getElementById('section-' + index).classList.add('active');

    // Xóa toàn bộ active ở các nút tab và nhóm
    document.querySelectorAll('.tab-btn').forEach(el => el.classList.remove('active'));
    document.querySelectorAll('.skill-group').forEach(el => el.classList.remove('active-skill'));

    // Kích hoạt tab được chọn và nhóm chứa nó
    let activeBtn = document.querySelector('.part-btn-' + index);
    if (activeBtn) {
        activeBtn.classList.add('active');
        activeBtn.closest('.skill-group').classList.add('active-skill');
    }

    currentIndex = index;
    document.getElementById('mainContent').scrollTop = 0;
}

// --- 3. ĐỒNG HỒ VÀ NỘP BÀI ---
//let timeLeft = @Model.DurationMinutes * 60;

// Flag chống double-submit (nhấn nút + hết giờ cùng lúc)
let isSubmitting = false;

function submitExam() {
    if (isSubmitting) return;

    Swal.fire({
        title: 'Nộp bài thi?',
        text: "Bạn có chắc chắn muốn kết thúc bài thi và nộp bài ngay bây giờ?",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#10b981',
        cancelButtonColor: '#6c757d',
        confirmButtonText: '<i class="bi bi-check2-circle me-1"></i> Xác nhận nộp bài',
        cancelButtonText: 'Tiếp tục làm bài',
        reverseButtons: true,
        customClass: {
            confirmButton: 'rounded-pill px-4 fw-bold',
            cancelButton: 'rounded-pill px-4'
        }
    }).then((result) => {
        if (result.isConfirmed) {
            isSubmitting = true;

            Swal.fire({
                title: 'Đang gửi bài làm...',
                html: 'Vui lòng chờ trong giây lát, hệ thống đang lưu kết quả và tệp tin âm thanh.',
                allowOutsideClick: false,
                didOpen: () => { Swal.showLoading(); }
            });

            document.body.style.opacity = '0.5';
            document.body.style.pointerEvents = 'none';
            //document.getElementById('examForm').submit();
            HTMLFormElement.prototype.submit.call(document.getElementById('examForm'));
            
        }
    });
}

// --- 4. GHI ÂM (SPEAKING) ---
let recorders = {};
let chunks = {};
async function toggleRecording(qId, qIdx) {
    const ui = document.getElementById(`recorder-${qId}`);
    const btn = ui.querySelector('.btn-record');
    const timerEl = document.getElementById(`timer-${qId}`);
    const statusText = ui.querySelector('.status-text');
    const fileInput = document.getElementById(`file-input-${qId}`);

    if (recorders[qId] && recorders[qId].state === "recording") {
        recorders[qId].stop();
        clearInterval(ui.dataset.timer);
        return;
    }

    if (fileInput.files.length > 0 && !confirm("Ghi âm lại sẽ xóa bản cũ?")) return;
    fileInput.files = new DataTransfer().files;

    try {
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        const mediaRecorder = new MediaRecorder(stream);
        recorders[qId] = mediaRecorder;
        chunks[qId] = [];

        mediaRecorder.ondataavailable = e => { if (e.data.size > 0) chunks[qId].push(e.data); };
        mediaRecorder.onstop = () => {
            const blob = new Blob(chunks[qId], { type: 'audio/webm' });
            const file = new File([blob], `ans_${qId}.webm`, { type: "audio/webm" });
            const container = new DataTransfer();
            container.items.add(file);
            fileInput.files = container.files;

            document.getElementById(`playback-${qId}`).src = URL.createObjectURL(blob);
            document.getElementById(`playback-${qId}`).classList.remove('d-none');

            btn.classList.replace('btn-danger', 'btn-outline-success');
            btn.innerHTML = '<i class="bi bi-arrow-counterclockwise fs-5"></i>';

            statusText.innerText = "Đã lưu bản thu thành công.";
            statusText.classList.add('text-success');
            statusText.classList.remove('text-danger', 'text-dark');
            stream.getTracks().forEach(track => track.stop());

            markAnswered(qIdx, true);
        };

        mediaRecorder.start();
        btn.classList.replace('btn-outline-danger', 'btn-danger');
        btn.innerHTML = '<i class="bi bi-stop-fill fs-5 text-white"></i>';

        statusText.innerText = "Đang ghi âm...";
        statusText.classList.add('text-danger');
        statusText.classList.remove('text-success', 'text-dark');

        let recTime = 0;
        timerEl.innerText = "00:00";
        ui.dataset.timer = setInterval(() => {
            recTime++;
            let rm = Math.floor(recTime / 60); let rs = recTime % 60;
            timerEl.innerText = `${rm < 10 ? '0' + rm : rm}:${rs < 10 ? '0' + rs : rs}`;
        }, 1000);
    } catch (err) { alert("Vui lòng cấp quyền Micro cho trình duyệt!"); }
}

// --- XỬ LÝ ĐẾM TỪ CHO BÀI VIẾT ---
function handleWritingInput(questionId, qIdx, text) {
    // Xóa các khoảng trắng thừa ở đầu/cuối và các khoảng trắng kép ở giữa
    let cleanText = text.trim().replace(/\s+/g, ' ');

    // Đếm số từ
    let wordCount = cleanText === "" ? 0 : cleanText.split(' ').length;

    // Hiển thị ra màn hình
    let countDisplay = document.getElementById('wordcount-' + questionId);
    if (countDisplay) {
        countDisplay.innerText = wordCount;
    }

    // Đánh dấu vào bản đồ câu hỏi (sidebar) nếu đã gõ nội dung
    markAnswered(qIdx, wordCount > 0);
}

// =====================
// XỬ LÝ AUDIO PLAYER (CHỈ NGHE 1 LẦN)
// =====================
//$(document).ready(function () {
//    // 1. Hàm khởi tạo và khôi phục trạng thái sau khi F5
//    function initAudioResumption() {
//        $("audio").each(function () {
//            const audioEl = this;
//            const audioId = audioEl.id;
//            const isEnded = localStorage.getItem("audio_ended_" + audioId);
//            const savedTime = localStorage.getItem("audio_current_" + audioId);
//            const $player = $(audioEl).closest(".mc-audio-player");

//            // [FIX] Dùng loadedmetadata để đảm bảo audio đã sẵn sàng trước khi set currentTime
//            // [FIX] Helper cập nhật toàn bộ UI theo currentTime hiện tại
//            function refreshUI() {
//                if (!audioEl.duration) return;
//                const t = audioEl.currentTime;
//                const dur = audioEl.duration;
//                const pct = (t / dur) * 100;
//                updateTimerDisplay($player, t);
//                $player.find(".mc-progress").css("width", pct + "%");
//                $player.find(".mc-duration").text(Math.floor(dur / 60) + ":" + String(Math.floor(dur % 60)).padStart(2, "0"));
//                $player.find(".waveform-bar").each(function (i) {
//                    const total = $player.find(".waveform-bar").length;
//                    $(this).css("background", (i / total) <= (pct / 100)
//                        ? "linear-gradient(180deg,#a5b4fc,#6366f1)"
//                        : "rgba(165,180,252,0.2)");
//                });
//            }

//            function applyRestore() {
//                if (isEnded === "true") {
//                    // Đã nghe xong: khoá UI và set progress 100%
//                    lockAudioUI($player);
//                    const dur = audioEl.duration || 0;
//                    $player.find(".mc-progress").css("width", "100%");
//                    $player.find(".mc-currenttime").text("Ended");
//                    $player.find(".mc-duration").text(dur > 0 ? (Math.floor(dur / 60) + ":" + String(Math.floor(dur % 60)).padStart(2, "0")) : "--:--");
//                } else if (savedTime) {
//                    const target = parseFloat(savedTime);
//                    // [FIX] Trình duyệt có thể bỏ qua seek ở loadedmetadata nếu buffer chưa sẵn sàng.
//                    // Dùng canplay (một lần) để đảm bảo seek thành công.
//                    function doSeek() {
//                        audioEl.currentTime = target;
//                        refreshUI();
//                    }
//                    if (audioEl.readyState >= 3) {
//                        // Đã buffer đủ → seek ngay
//                        doSeek();
//                    } else {
//                        // Chờ canplay để seek
//                        audioEl.addEventListener("canplay", function onCanPlay() {
//                            audioEl.removeEventListener("canplay", onCanPlay);
//                            doSeek();
//                        });
//                    }
//                }
//            }

//            if (audioEl.readyState >= 1) {
//                applyRestore();
//            } else {
//                audioEl.addEventListener("loadedmetadata", applyRestore, { once: true });
//            }
//        });
//    }

//    // 2. Xử lý nút Play
//    $(document).on("click", ".mc-play", function () {
//        const $btn = $(this);
//        const $player = $btn.closest(".mc-audio-player");
//        const audio = $player.find("audio")[0];
//        const audioId = audio.id;

//        if (localStorage.getItem("audio_ended_" + audioId) === "true") {
//            alert("⚠️ Bạn đã hoàn thành bài nghe này.");
//            return false;
//        }

//        if (audio.paused) {
//            // Dừng các bài khác
//            $("audio").each(function () { this.pause(); });
//            $(".mc-play").css("background", "linear-gradient(135deg,#6366f1,#8b5cf6)")
//                .find("i").attr("class", "bi bi-play-fill");

//            audio.play().catch(err => console.error("Lỗi phát audio:", err));
//            $btn.css("background", "linear-gradient(135deg,#7c3aed,#a78bfa)")
//                .find("i").attr("class", "bi bi-pause-fill");
//        } else {
//            // Cho phép tạm dừng
//            audio.pause();
//            $btn.css("background", "linear-gradient(135deg,#6366f1,#8b5cf6)")
//                .find("i").attr("class", "bi bi-play-fill");
//        }
//    });

//    // 3. Theo dõi tiến trình & Chạy đồng hồ
//    $(document).on("timeupdate", "audio", function () {
//        const audio = this;
//        const $player = $(audio).closest(".mc-audio-player");
//        if (!$player.length) return;

//        if (!isNaN(audio.duration) && audio.duration > 0) {
//            const percent = (audio.currentTime / audio.duration) * 100;

//            // Thanh progress
//            $player.find(".mc-progress").css("width", percent + "%");

//            // Đồng hồ hiện tại
//            updateTimerDisplay($player, audio.currentTime);

//            // Duration (nếu chưa hiển thị)
//            const $dur = $player.find(".mc-duration");
//            if ($dur.text() === "--:--" || $dur.text() === "") {
//                $dur.text(Math.floor(audio.duration / 60) + ":" + String(Math.floor(audio.duration % 60)).padStart(2, "0"));
//            }

//            // Waveform bars (nếu có)
//            const bars = $player.find(".waveform-bar");
//            if (bars.length) {
//                bars.each(function (i) {
//                    $(this).css("background", (i / bars.length) <= (percent / 100)
//                        ? "linear-gradient(180deg,#a5b4fc,#6366f1)"
//                        : "rgba(165,180,252,0.2)");
//                });
//            }

//            // LƯU VÀO CACHE: vị trí hiện tại để F5 nghe tiếp
//            try { localStorage.setItem("audio_current_" + audio.id, audio.currentTime); } catch (e) { }
//        }
//    });

//    // 4. Khi bài nghe kết thúc
//    $(document).on("ended", "audio", function () {
//        const audioId = this.id;
//        const $player = $(this).closest(".mc-audio-player");

//        localStorage.setItem("audio_ended_" + audioId, "true");
//        localStorage.removeItem("audio_current_" + audioId); // Xóa vị trí lưu tạm

//        lockAudioUI($player);
//        alert("✓ Bài nghe đã kết thúc.");
//    });

//    // --- Hàm Helper hỗ trợ giao diện ---
//    function updateTimerDisplay($player, time) {
//        const m = Math.floor(time / 60);
//        const s = Math.floor(time % 60);
//        $player.find(".mc-currenttime").text(`${m}:${s < 10 ? '0' + s : s}`);
//    }

//    function lockAudioUI($player) {
//        const $btn = $player.find(".mc-play");
//        $btn.prop("disabled", true)
//            .css({ background: "linear-gradient(135deg,#6b7280,#9ca3af)", opacity: "0.6", boxShadow: "none" })
//            .find("i").attr("class", "bi bi-lock-fill");
//        $player.find(".mc-rewind, .mc-forward").prop("disabled", true).css("opacity", "0.3");
//        const $badge = $player.find(".mc-listen-badge");
//        if ($badge.length) $badge.text("ĐÃ NGHE XONG");
//    }

//    // 5. Nút tua lại 10 giây
//    $(document).on("click", ".mc-rewind", function () {
//        const audio = $(this).closest(".mc-audio-player").find("audio")[0];
//        if (audio) audio.currentTime = Math.max(0, audio.currentTime - 10);
//    });

//    // 6. Nút tua tới 10 giây
//    $(document).on("click", ".mc-forward", function () {
//        const audio = $(this).closest(".mc-audio-player").find("audio")[0];
//        if (audio) audio.currentTime = Math.min(audio.duration || 0, audio.currentTime + 10);
//    });

//    // Thực thi khi load trang
//    initAudioResumption();
//});

$(document).ready(function () {
    // 1. Khôi phục trạng thái Audio sau khi F5
    function initAudioResumption() {
        $("audio.mc-audio-element").each(function () {
            const audioEl = this;
            const audioId = audioEl.id;
            const isEnded = localStorage.getItem("audio_ended_" + audioId);
            const savedTime = localStorage.getItem("audio_current_" + audioId);
            const $player = $(audioEl).closest(".mc-audio-player");

            // Nếu đã nghe xong hoàn toàn
            if (isEnded === "true") {
                lockAudioUI($player, "ĐÃ HOÀN THÀNH");
                $player.find(".mc-progress").css("width", "100%");
                $player.find(".mc-currenttime").text("Ended");
                return;
            }

            // Nếu có thời gian lưu trữ (đã từng nghe và bị F5)
            if (savedTime && parseFloat(savedTime) > 0) {
                audioEl.addEventListener("loadedmetadata", function () {
                    audioEl.currentTime = parseFloat(savedTime);
                    updateUIPos($player, audioEl.currentTime, audioEl.duration);
                }, { once: true });
            }
        });
    }

    // Cập nhật giao diện vị trí thời gian và waveform
    function updateUIPos($player, current, duration) {
        if (!duration) return;
        const pct = (current / duration) * 100;
        $player.find(".mc-progress").css("width", pct + "%");
        $player.find(".mc-currenttime").text(formatTime(current));
        $player.find(".mc-duration").text(formatTime(duration));

        // Tô màu Waveform theo tiến độ
        const bars = $player.find(".waveform-bar");
        const activeCount = Math.floor((pct / 100) * bars.length);
        bars.each(function (i) {
            $(this).css("background", i <= activeCount ? "#818cf8" : "rgba(148, 163, 184, 0.2)");
        });
    }

    function formatTime(seconds) {
        const m = Math.floor(seconds / 60);
        const s = Math.floor(seconds % 60);
        return `${String(m).padStart(2, '0')}:${String(s).padStart(2, '0')}`;
    }

    function lockAudioUI($player, message) {
        const $btn = $player.find(".mc-play");
        $btn.prop("disabled", true).css({ "background": "#475569", "opacity": "0.7", "cursor": "not-allowed" });
        $btn.find("i").attr("class", "bi bi-lock-fill");
        $player.find(".mc-listen-badge").text(message).css({ "background": "rgba(148,163,184,0.1)", "color": "#94a3b8" });
        $player.find(".mc-rewind, .mc-forward").hide();
    }

    // 2. Sự kiện nhấn nút Play/Pause
    $(document).on("click", ".mc-play", function () {
        const $btn = $(this);
        const $player = $btn.closest(".mc-audio-player");
        const audio = $player.find("audio")[0];

        if (audio.paused) {
            // Dừng các audio khác đang phát
            $("audio").each(function () { if (this !== audio) this.pause(); });
            $(".mc-play i").attr("class", "bi bi-play-fill");

            audio.play().catch(e => console.error(e));
            $btn.find("i").attr("class", "bi bi-pause-fill");
            $btn.css("background", "#ef4444"); // Đổi sang màu đỏ khi đang phát
        } else {
            audio.pause();
            $btn.find("i").attr("class", "bi bi-play-fill");
            $btn.css("background", "#6366f1");
        }
    });

    // 3. Theo dõi thời gian thực (TimeUpdate)
    $(document).on("timeupdate", "audio.mc-audio-element", function () {
        const audio = this;
        const $player = $(audio).closest(".mc-audio-player");

        if (!isNaN(audio.duration)) {
            updateUIPos($player, audio.currentTime, audio.duration);
            // Lưu vị trí hiện tại để không cho nghe lại từ đầu nếu F5
            localStorage.setItem("audio_current_" + audio.id, audio.currentTime);
        }
    });

    // 4. Kết thúc audio
    $(document).on("ended", "audio.mc-audio-element", function () {
        const audioId = this.id;
        const $player = $(this).closest(".mc-audio-player");
        localStorage.setItem("audio_ended_" + audioId, "true");
        localStorage.removeItem("audio_current_" + audioId);
        lockAudioUI($player, "HOÀN THÀNH");
        Swal.fire({ icon: 'success', title: 'Hoàn thành bài nghe', text: 'Bạn đã nghe xong đoạn hội thoại này.', timer: 2000 });
    });

    // 5. Xử lý tua (Chặn không cho lùi về sau vị trí đã nghe - tùy chọn bảo mật)
    $(document).on("click", ".mc-rewind", function () {
        const audio = $(this).closest(".mc-audio-player").find("audio")[0];
        // Chỉ cho lùi tối đa 10s nhưng không được về 0 nếu đề bài yêu cầu khắt khe
        audio.currentTime = Math.max(0, audio.currentTime - 10);
    });

    $(document).on("click", ".mc-forward", function () {
        const audio = $(this).closest(".mc-audio-player").find("audio")[0];
        audio.currentTime = Math.min(audio.duration, audio.currentTime + 10);
    });

    initAudioResumption();
});


// Đồng hồ đếm ngược
const timerInterval = setInterval(function () {
    if (timeLeft <= 0) {
        clearInterval(timerInterval);
        if (isSubmitting) return; // đã nộp rồi, không làm gì thêm
        isSubmitting = true;

        // Dừng auto-save trước khi nộp
        if (typeof AutoSave !== 'undefined') {
            AutoSave.stopAutoSave();
            AutoSave.clearDraft();
        }

        // Đếm ngược 5 giây trước khi tự động nộp
        let countdown = 5;
        Swal.fire({
            title: 'Hết giờ thi!',
            html: `Thời gian làm bài đã hết.<br>Hệ thống sẽ tự động nộp bài sau <b id="swal-countdown">${countdown}</b> giây...`,
            icon: 'warning',
            allowOutsideClick: false,
            showConfirmButton: false,
            didOpen: () => {
                const countdownEl = document.getElementById('swal-countdown');
                const tick = setInterval(() => {
                    countdown--;
                    if (countdownEl) countdownEl.textContent = countdown;
                    //if (countdown <= 0) {
                    //    clearInterval(tick);
                    //    Swal.close();
                    //    //document.getElementById('examForm').submit();
                    //    HTMLFormElement.prototype.submit.call(document.getElementById('examForm'));
                    //}
                    if (countdown <= 0) {
                        clearInterval(tick);

                        // 1. Hiển thị hộp thoại loading giống hệt nút nộp bài thủ công
                        Swal.fire({
                            title: 'Đang gửi bài làm...',
                            html: 'Vui lòng chờ trong giây lát, hệ thống đang lưu kết quả và tệp tin âm thanh.',
                            allowOutsideClick: false,
                            showConfirmButton: false,
                            didOpen: () => { Swal.showLoading(); }
                        });

                        // 2. Khóa tương tác màn hình
                        document.body.style.opacity = '0.5';
                        document.body.style.pointerEvents = 'none';

                        // 3. Thực hiện nộp form
                        HTMLFormElement.prototype.submit.call(document.getElementById('examForm'));
                    }
                }, 1000);
            }
        });
        return;
    }


    // Hiển thị timer — hỗ trợ cả id="countdown" (Take) lẫn id="timer" (TestTake)
    const timerDisplayEl = document.getElementById('timer') || document.getElementById('countdown');
    if (timerDisplayEl) {
        const h = Math.floor(timeLeft / 3600);
        const m = Math.floor((timeLeft % 3600) / 60);
        const s = timeLeft % 60;
        timerDisplayEl.innerText = h > 0
            ? `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`
            : `${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
    }

    timeLeft--;
    // [FIX] Lưu thời gian còn lại để khôi phục sau F5
    try { localStorage.setItem('exam_timeleft', timeLeft); } catch (e) { }
}, 1000);
