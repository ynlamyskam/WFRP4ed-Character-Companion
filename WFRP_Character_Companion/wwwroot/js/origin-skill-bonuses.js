(function () {
    const MAX_EACH = 3;

    document.querySelectorAll('[data-origin-skill-bonuses]').forEach((form) => {
        const count3 = form.querySelector('[data-count-plus3]');
        const count5 = form.querySelector('[data-count-plus5]');

        function getRows() {
            return Array.from(form.querySelectorAll('[data-skill-row]'));
        }

        function selectedBonus(row) {
            return row.querySelector('input[type="radio"]:checked')?.value || '';
        }

        function countSelected(value) {
            return getRows().filter(r => selectedBonus(r) === value).length;
        }

        function updateCounts() {
            const n3 = countSelected('3');
            const n5 = countSelected('5');

            if (count3) {
                count3.textContent = String(n3);
                count3.classList.toggle('text-emerald-400', n3 === MAX_EACH);
                count3.classList.toggle('text-red-400', n3 > MAX_EACH);
            }
            if (count5) {
                count5.textContent = String(n5);
                count5.classList.toggle('text-emerald-400', n5 === MAX_EACH);
                count5.classList.toggle('text-red-400', n5 > MAX_EACH);
            }

            getRows().forEach(row => {
                const current = selectedBonus(row);
                row.querySelectorAll('input[type="radio"]').forEach(radio => {
                    const pool = radio.value === '3' ? n3 : n5;
                    const atCap = pool >= MAX_EACH;
                    radio.disabled = current !== radio.value && atCap;
                });
            });
        }

        form.querySelectorAll('input[type="radio"][name^="skill_bonus_"]').forEach(radio => {
            radio.addEventListener('mousedown', () => {
                radio.dataset.pendingUncheck = radio.checked ? '1' : '0';
            });

            radio.addEventListener('click', (e) => {
                if (radio.dataset.pendingUncheck === '1') {
                    radio.checked = false;
                    radio.dataset.pendingUncheck = '0';
                    e.preventDefault();
                    updateCounts();
                }
            });

            radio.addEventListener('change', () => {
                const row = radio.closest('[data-skill-row]');
                const val = radio.value;

                if (val === '3' || val === '5') {
                    const otherVal = val === '3' ? '5' : '3';
                    const other = row?.querySelector(`input[type="radio"][value="${otherVal}"]`);
                    if (other?.checked) other.checked = false;
                }

                const n = countSelected(val);
                if (n > MAX_EACH) {
                    radio.checked = false;
                }

                updateCounts();
            });
        });

        form.addEventListener('submit', (e) => {
            const n3 = countSelected('3');
            const n5 = countSelected('5');

            for (const row of getRows()) {
                const customInput = row.querySelector('[data-custom-spec]');
                if (!customInput) continue;
                const bonus = selectedBonus(row);
                if (bonus && !customInput.value.trim()) {
                    e.preventDefault();
                    customInput.focus();
                    customInput.classList.add('border-red-600');
                    return;
                }
            }

            if (n3 > MAX_EACH || n5 > MAX_EACH) {
                e.preventDefault();
            }
        });

        updateCounts();
    });
})();
