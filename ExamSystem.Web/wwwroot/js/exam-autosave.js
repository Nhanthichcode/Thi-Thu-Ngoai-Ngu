// =====================================================
// PHẦN 1: AUDIODB - LƯU FILE ÂM THANH VÀO INDEXEDDB
// =====================================================
const AudioDB = {
    DB_NAME: 'ExamAudioDraft',
    DB_VERSION: 1,
    STORE_NAME: 'audioBlobs',
    db: null,

    // Mở kết nối IndexedDB
    open: function () {
        return new Promise((resolve, reject) => {
            if (this.db) { resolve(this.db); return; }

            const req = indexedDB.open(this.DB_NAME, this.DB_VERSION);

            req.onupgradeneeded = (e) => {
                const db = e.target.result;
                if (!db.objectStoreNames.contains(this.STORE_NAME)) {
                    db.createObjectStore(this.STORE_NAME);
                }
            };

            req.onsuccess = (e) => {
                this.db = e.target.result;
                resolve(this.db);
            };

            req.onerror = (e) => {
                console.error('❌ IndexedDB open failed:', e.target.error);
                reject(e.target.error);
            };
        });
    },

    // Lưu blob âm thanh
    // key ví dụ: "exam_audio_3_userId_questionId"
    save: async function (key, blob) {
        try {
            const db = await this.open();
            return new Promise((resolve, reject) => {
                const tx = db.transaction(this.STORE_NAME, 'readwrite');
                tx.objectStore(this.STORE_NAME).put(blob, key);
                tx.oncomplete = () => { console.log('🎤 Audio saved to IndexedDB:', key); resolve(); };
                tx.onerror = (e) => reject(e.target.error);
            });
        } catch (err) {
            console.error('❌ AudioDB.save failed:', err);
        }
    },

    // Lấy blob âm thanh
    get: async function (key) {
        try {
            const db = await this.open();
            return new Promise((resolve, reject) => {
                const tx = db.transaction(this.STORE_NAME, 'readonly');
                const req = tx.objectStore(this.STORE_NAME).get(key);
                req.onsuccess = () => resolve(req.result || null);
                req.onerror = (e) => reject(e.target.error);
            });
        } catch (err) {
            console.error('❌ AudioDB.get failed:', err);
            return null;
        }
    },

    // Lấy tất cả key có prefix nhất định
    getAllKeys: async function (prefix) {
        try {
            const db = await this.open();
            return new Promise((resolve, reject) => {
                const tx = db.transaction(this.STORE_NAME, 'readonly');
                const req = tx.objectStore(this.STORE_NAME).getAllKeys();
                req.onsuccess = () => {
                    const keys = (req.result || []).filter(k => k.startsWith(prefix));
                    resolve(keys);
                };
                req.onerror = (e) => reject(e.target.error);
            });
        } catch (err) {
            console.error('❌ AudioDB.getAllKeys failed:', err);
            return [];
        }
    },

    // Xóa một key
    delete: async function (key) {
        try {
            const db = await this.open();
            return new Promise((resolve, reject) => {
                const tx = db.transaction(this.STORE_NAME, 'readwrite');
                tx.objectStore(this.STORE_NAME).delete(key);
                tx.oncomplete = () => resolve();
                tx.onerror = (e) => reject(e.target.error);
            });
        } catch (err) {
            console.error('❌ AudioDB.delete failed:', err);
        }
    },

    // Xóa tất cả audio của một bài thi
    clearAll: async function (prefix) {
        const keys = await this.getAllKeys(prefix);
        for (const key of keys) {
            await this.delete(key);
        }
        console.log(`🗑️ Cleared ${keys.length} audio draft(s) from IndexedDB`);
    }
};


// =====================================================
// PHẦN 2: AUTOSAVE - LÕI CHÍNH
// =====================================================
const AutoSave = {
    SAVE_INTERVAL: 10000, // 30 giây
    storageKey: null,
    audioKeyPrefix: null,
    examId: null,
    saveTimer: null,
    lastSaveTime: null,

    // ---------------------------------------------------
    // KHỞI TẠO
    // ---------------------------------------------------
    init: function (examId) {
        this.examId = examId;
        // userId là biến global được inject từ Razor View
        this.storageKey = `exam_draft_${examId}_${userId}`;
        this.audioKeyPrefix = `exam_audio_${examId}_${userId}_`;

        console.log('🔧 Auto-save initialized for Exam ID:', examId);

        this.restoreDraft();
        this.startAutoSave();
        this.setupBeforeUnload();
        this.showSaveStatus();
    },

    // ---------------------------------------------------
    // THU THẬP ĐÁP ÁN (trắc nghiệm + tự luận)
    // ---------------------------------------------------
    collectAnswers: function () {
        const data = {
            examId: this.examId,
            timestamp: new Date().toISOString(),
            answers: {},  // { questionId: answerId }
            essays: {},   // { questionId: text }
            audioIds: []  // questionId nào có ghi âm (blob lưu riêng IndexedDB)
        };

        // Trắc nghiệm
        $('input[type="radio"]:checked').each(function () {
            const name = $(this).attr('name');
            const match = name.match(/answers\[(\d+)\]/);
            if (match) data.answers[match[1]] = parseInt($(this).val());
        });

        // Tự luận (Writing)
        $('textarea[name^="essayAnswers"]').each(function () {
            const name = $(this).attr('name');
            const match = name.match(/essayAnswers\[(\d+)\]/);
            if (match) {
                const text = $(this).val().trim();
                if (text.length > 0) data.essays[match[1]] = text;
            }
        });

        // Ghi lại các questionId đã có audio (để biết khi restore)
        $('input[type="file"][name^="audioAnswers"]').each(function () {
            if (this.files && this.files.length > 0) {
                const name = $(this).attr('name');
                const match = name.match(/audioAnswers\[(\d+)\]/);
                if (match) data.audioIds.push(match[1]);
            }
        });

        return data;
    },

    // ---------------------------------------------------
    // LƯU ĐÁP ÁN (localStorage) + AUDIO (IndexedDB)
    // ---------------------------------------------------
    saveDraft: async function () {
        try {
            const data = this.collectAnswers();
            const totalAnswers = Object.keys(data.answers).length
                + Object.keys(data.essays).length
                + data.audioIds.length;

            if (totalAnswers === 0) {
                console.log('⏭️ Skip save - No answers yet');
                return;
            }

            // Lưu text vào localStorage
            localStorage.setItem(this.storageKey, JSON.stringify(data));

            // Lưu audio vào IndexedDB
            const audioSavePromises = [];
            $('input[type="file"][name^="audioAnswers"]').each((_, el) => {
                if (el.files && el.files.length > 0) {
                    const name = el.getAttribute('name');
                    const match = name.match(/audioAnswers\[(\d+)\]/);
                    if (match) {
                        const qId = match[1];
                        const blob = el.files[0];
                        const audioKey = `${this.audioKeyPrefix}${qId}`;
                        audioSavePromises.push(AudioDB.save(audioKey, blob));
                    }
                }
            });
            await Promise.all(audioSavePromises);

            this.lastSaveTime = new Date();
            console.log('💾 Auto-saved:', totalAnswers, 'answers at', this.lastSaveTime.toLocaleTimeString());
            this.updateSaveIndicator('success');

        } catch (error) {
            console.error('❌ Save failed:', error);
            this.updateSaveIndicator('error');
        }
    },

    // ---------------------------------------------------
    // KHÔI PHỤC BÀI LÀM
    // ---------------------------------------------------
    restoreDraft: function () {
        try {
            const saved = localStorage.getItem(this.storageKey);
            if (!saved) {
                console.log('ℹ️ No draft found');
                return;
            }

            const data = JSON.parse(saved);
            const totalSaved = Object.keys(data.answers).length
                + Object.keys(data.essays).length
                + (data.audioIds ? data.audioIds.length : 0);

            Swal.fire({
                title: 'Phát hiện bài làm chưa hoàn tất',
                html: `Bạn có muốn khôi phục bài làm trước đó?<br>
                       <small class="text-muted">Lưu lúc: ${new Date(data.timestamp).toLocaleString('vi-VN')}</small><br>
                       <small class="text-muted">Tổng cộng: ${totalSaved} câu trả lời${data.audioIds && data.audioIds.length > 0 ? ` (gồm ${data.audioIds.length} câu ghi âm)` : ''}</small>`,
                icon: 'question',
                showCancelButton: true,
                confirmButtonText: 'Khôi phục',
                cancelButtonText: 'Làm mới',
                confirmButtonColor: '#10b981',
                cancelButtonColor: '#ef4444'
            }).then(async (result) => {
                if (result.isConfirmed) {
                    let restoredCount = 0;

                    // 0. Khôi phục timeLeft (đồng hồ đếm ngược)
                    const timerKey = 'exam_timer_' + this.examId;
                    const savedTimer = localStorage.getItem(timerKey);
                    if (savedTimer !== null && parseInt(savedTimer) > 0) {
                        timeLeft = parseInt(savedTimer);
                        console.log('⏱️ Timer restored:', timeLeft, 'seconds');
                    }

                    // 1. Khôi phục trắc nghiệm
                    Object.entries(data.answers).forEach(([qId, answerId]) => {
                        const radio = $(`input[name="answers[${qId}]"][value="${answerId}"]`);
                        if (radio.length) {
                            radio.prop('checked', true);
                            restoredCount++;

                            // Cập nhật sidebar map
                            const qIdx = radio.closest('.question-card').find('[data-question-index]').data('question-index')
                                || this._getQuestionIndex(radio.closest('.question-card'));
                            if (qIdx) markAnswered(qIdx, true);
                        }
                    });

                    // 2. Khôi phục tự luận (Writing)
                    Object.entries(data.essays).forEach(([qId, text]) => {
                        const textarea = $(`textarea[name="essayAnswers[${qId}]"]`);
                        if (textarea.length) {
                            textarea.val(text);
                            restoredCount++;

                            // Cập nhật word count + sidebar map
                            const qIdx = this._getQuestionIndex(textarea.closest('.question-card'));
                            if (qIdx) {
                                handleWritingInput(qId, qIdx, text);
                                markAnswered(qIdx, true);
                            }
                        }
                    });

                    // 3. Khôi phục audio (Speaking)
                    if (data.audioIds && data.audioIds.length > 0) {
                        const audioRestorePromises = data.audioIds.map(async (qId) => {
                            const audioKey = `${this.audioKeyPrefix}${qId}`;
                            const blob = await AudioDB.get(audioKey);
                            if (!blob) return;

                            // Inject vào <input type="file">
                            const fileInput = document.getElementById(`file-input-${qId}`);
                            if (fileInput) {
                                const mimeType = blob.type || 'audio/webm';
                                const ext = mimeType.split('/')[1] || 'webm';
                                const file = new File([blob], `speaking_${qId}.${ext}`, { type: mimeType });
                                const dt = new DataTransfer();
                                dt.items.add(file);
                                fileInput.files = dt.files;
                            }

                            // Cập nhật audio playback UI
                            const objectUrl = URL.createObjectURL(blob);
                            const audioEl = document.getElementById(`playback-${qId}`);
                            if (audioEl) {
                                audioEl.src = objectUrl;
                                audioEl.classList.remove('d-none');
                            }

                            // Cập nhật status text và nút ghi âm
                            const recorderDiv = document.getElementById(`recorder-${qId}`);
                            if (recorderDiv) {
                                const statusEl = recorderDiv.querySelector('.status-text');
                                if (statusEl) statusEl.textContent = '✅ Đã có bài ghi âm (khôi phục)';

                                const btnRecord = recorderDiv.querySelector('.btn-record');
                                if (btnRecord) {
                                    btnRecord.classList.remove('btn-outline-danger');
                                    btnRecord.classList.add('btn-outline-warning');
                                    btnRecord.title = 'Ghi âm lại sẽ ghi đè bài cũ';
                                }
                            }

                            // Cập nhật sidebar map
                            const recorderWrapper = $(`#recorder-${qId}`).closest('.question-card');
                            const qIdx = this._getQuestionIndex(recorderWrapper);
                            if (qIdx) markAnswered(qIdx, true);

                            restoredCount++;
                            console.log(`🎤 Audio restored for question ${qId}`);
                        });

                        await Promise.all(audioRestorePromises);
                    }

                    console.log('✅ Restored', restoredCount, 'answers');
                    Swal.fire({
                        title: 'Khôi phục thành công!',
                        text: `Đã khôi phục ${restoredCount} câu trả lời`,
                        icon: 'success',
                        timer: 2000,
                        showConfirmButton: false
                    });

                } else {
                    // Người dùng chọn Làm mới → xóa toàn bộ (kể cả timer)
                    this.clearDraft();
                    localStorage.removeItem('exam_timer_' + this.examId);
                    console.log('🗑️ Draft + timer cleared by user');
                }
            });

        } catch (error) {
            console.error('❌ Restore failed:', error);
            this.clearDraft();
        }
    },

    // ---------------------------------------------------
    // HELPER: Lấy question index từ DOM
    // ---------------------------------------------------
    // Các câu hỏi có id="q-wrapper-N" → đọc N từ đó
    _getQuestionIndex: function (cardEl) {
        if (!cardEl || !cardEl.length) return null;
        const id = cardEl.attr('id') || '';                   // "q-wrapper-5"
        const match = id.match(/q-wrapper-(\d+)/);
        return match ? parseInt(match[1]) : null;
    },

    // ---------------------------------------------------
    // AUTO-SAVE TIMER
    // ---------------------------------------------------
    startAutoSave: function () {
        setTimeout(() => this.saveDraft(), 10000);
        this.saveTimer = setInterval(() => { this.saveDraft(); }, this.SAVE_INTERVAL);
        console.log('⏰ Auto-save started - every 30 seconds');
    },

    stopAutoSave: function () {
        if (this.saveTimer) {
            clearInterval(this.saveTimer);
            console.log('⏹️ Auto-save stopped');
        }
    },

    // ---------------------------------------------------
    // XÓA DRAFT (cả localStorage lẫn IndexedDB)
    // ---------------------------------------------------
    clearDraft: async function () {
        localStorage.removeItem(this.storageKey);
        localStorage.removeItem('exam_timer_' + this.examId);
        await AudioDB.clearAll(this.audioKeyPrefix);
        console.log('🗑️ All drafts cleared');
    },

    // ---------------------------------------------------
    // TRƯỚC KHI RỜI TRANG
    // ---------------------------------------------------
    setupBeforeUnload: function () {
        window.addEventListener('beforeunload', () => {
            this.saveDraft(); // async nhưng trình duyệt sẽ cố chạy
        });
    },

    // ---------------------------------------------------
    // HIỂN THỊ TRẠNG THÁI SAVE
    // ---------------------------------------------------
    showSaveStatus: function () {
        const html = `
            <div id="autosave-indicator" style="
                position: fixed; bottom: 20px; right: 20px;
                padding: 8px 16px; background: white;
                border-radius: 20px; box-shadow: 0 2px 8px rgba(0,0,0,0.15);
                font-size: 13px; z-index: 9999;
                display: none; align-items: center; gap: 8px;">
                <i class="bi bi-cloud-check-fill text-success"></i>
                <span>Đã lưu tự động</span>
            </div>`;
        $('body').append(html);
    },

    updateSaveIndicator: function (status) {
        const indicator = $('#autosave-indicator');
        if (status === 'success') {
            indicator.html('<i class="bi bi-cloud-check-fill text-success"></i><span>Đã lưu tự động</span>');
            indicator.css('display', 'flex').fadeIn(200);
            setTimeout(() => indicator.fadeOut(300), 3000);
        } else if (status === 'error') {
            indicator.html('<i class="bi bi-cloud-slash-fill text-danger"></i><span>Lỗi lưu bài</span>');
            indicator.css('display', 'flex').fadeIn(200);
        }
    }
};


// =====================================================
// PHẦN 3: HOOK VÀO submitExam() CỦA student.js
// — Không override toàn bộ, chỉ chèn thêm clearDraft trước khi submit —
// =====================================================
(function () {
    // Giữ lại hàm gốc từ student.js (đã có isSubmitting guard + Swal xác nhận)
    const _originalSubmit = window.submitExam;

    window.submitExam = function () {
        // Gọi hàm gốc; AutoSave.clearDraft sẽ chạy trong .then() của Swal
        // bên trong student.js sau khi người dùng xác nhận.
        // Ta chỉ cần patch phần "isConfirmed" bằng cách wrap onstop của form.
        _originalSubmit();
    };

    // Lắng nghe sự kiện submit của form — tại thời điểm này draft chắc chắn cần xóa
    document.addEventListener('DOMContentLoaded', () => {
        const form = document.getElementById('examForm');
        if (form) {
            form.addEventListener('submit', async () => {
                AutoSave.stopAutoSave();
                await AutoSave.clearDraft(); // xóa cả audio IndexedDB
            });
        }
    });
})();


// =====================================================
// PHẦN 4: HẾT GIỜ — đã xử lý bởi timerInterval trong student.js
// AutoSave.stopAutoSave() và AutoSave.clearDraft() được gọi ở đó.
// File này không cần override lại để tránh conflict.
// =====================================================


console.log('📦 Auto-save module (with Audio + Sidebar restore) loaded successfully');