(function () {
    document.querySelectorAll('[data-rearrange-rolls]').forEach((form) => {
        const poolEl = form.querySelector('[data-roll-pool]');
        const slots = Array.from(form.querySelectorAll('[data-roll-slot]'));
        const hiddenInputs = Array.from(form.querySelectorAll('input[name^="rollIndex_"]'));
        if (!poolEl || !slots.length) return;

        const rolls = JSON.parse(form.dataset.rolls || '[]');
        let selectedRoll = null;

        const assignments = new Array(slots.length).fill(null);

        function renderPool() {
            poolEl.innerHTML = '';
            const used = new Set(assignments.filter(v => v !== null));
            rolls.forEach((value, index) => {
                if (used.has(index)) return;
                const chip = document.createElement('button');
                chip.type = 'button';
                chip.dataset.rollIndex = String(index);
                chip.className = 'rounded-lg border border-amber-700/60 bg-amber-950/40 px-4 py-2 font-mono text-lg text-amber-300 transition hover:border-amber-500 hover:bg-amber-900/50';
                chip.textContent = String(value);
                if (selectedRoll === index) {
                    chip.classList.add('ring-2', 'ring-amber-400');
                }
                chip.addEventListener('click', () => {
                    selectedRoll = selectedRoll === index ? null : index;
                    renderPool();
                    renderSlots();
                });
                poolEl.appendChild(chip);
            });
            if (!poolEl.children.length) {
                const done = document.createElement('p');
                done.className = 'text-sm text-emerald-400';
                done.textContent = 'Wszystkie rzuty przypisane.';
                poolEl.appendChild(done);
            }
        }

        function renderSlots() {
            slots.forEach((slot, attrIndex) => {
                const rollIndex = assignments[attrIndex];
                const display = slot.querySelector('[data-roll-display]');
                const hidden = hiddenInputs[attrIndex];
                if (rollIndex === null) {
                    display.textContent = '—';
                    display.className = 'flex min-h-[2.5rem] min-w-[3rem] items-center justify-center rounded-lg border border-dashed border-stone-600 font-mono text-stone-500';
                    if (hidden) hidden.value = '';
                } else {
                    display.textContent = String(rolls[rollIndex]);
                    display.className = 'flex min-h-[2.5rem] min-w-[3rem] cursor-pointer items-center justify-center rounded-lg border border-amber-600 bg-amber-950/50 font-mono text-lg text-amber-300 transition hover:border-amber-400';
                    if (hidden) hidden.value = String(rollIndex);
                }
            });
        }

        slots.forEach((slot, attrIndex) => {
            slot.addEventListener('click', () => {
                if (selectedRoll !== null) {
                    const prev = assignments[attrIndex];
                    assignments[attrIndex] = selectedRoll;
                    selectedRoll = prev;
                    renderPool();
                    renderSlots();
                    return;
                }
                if (assignments[attrIndex] !== null) {
                    selectedRoll = assignments[attrIndex];
                    assignments[attrIndex] = null;
                    renderPool();
                    renderSlots();
                }
            });
        });

        renderPool();
        renderSlots();
    });
})();
