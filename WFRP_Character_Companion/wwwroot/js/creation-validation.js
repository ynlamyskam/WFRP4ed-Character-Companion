(function () {
    function showFormError(form, message) {
        let box = form.querySelector('[data-client-error]');
        if (!box) {
            box = document.createElement('div');
            box.setAttribute('data-client-error', '');
            box.className = 'mb-4 flex items-start gap-3 rounded-lg border border-red-800/60 bg-red-950/50 px-4 py-3 text-sm text-red-200';
            form.prepend(box);
        }
        box.textContent = message;
        box.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    function clearFormError(form) {
        form.querySelector('[data-client-error]')?.remove();
    }

    function sumInputs(form, selector) {
        return Array.from(form.querySelectorAll(selector)).reduce((acc, el) => {
            const v = parseInt(el.value, 10);
            return acc + (Number.isFinite(v) ? v : 0);
        }, 0);
    }

    function bindExactSum(form, inputSelector, target, message) {
        form.addEventListener('submit', (e) => {
            clearFormError(form);
            const total = sumInputs(form, inputSelector);
            if (total !== target) {
                e.preventDefault();
                showFormError(form, message.replace('{total}', total).replace('{target}', target));
            }
        });
    }

    document.querySelectorAll('[data-validate="advance-points"]').forEach((form) => {
        const target = parseInt(form.dataset.target || '5', 10);
        bindExactSum(
            form,
            'input[name^="advance_"]',
            target,
            'Rozdzielono {total} punktów — wymagane dokładnie {target}.'
        );
    });

    document.querySelectorAll('[data-validate="profession-points"]').forEach((form) => {
        bindExactSum(
            form,
            'input[name^="pts_"]',
            40,
            'Rozdzielono {total} punktów — wymagane dokładnie {target}.'
        );
    });

    document.querySelectorAll('[data-validate="fate-pool"]').forEach((form) => {
        const pool = parseInt(form.dataset.pool || '0', 10);
        form.addEventListener('submit', (e) => {
            clearFormError(form);
            const fate = parseInt(form.querySelector('[name="FateBonus"]')?.value || '0', 10) || 0;
            const hero = parseInt(form.querySelector('[name="ResilienceBonus"]')?.value || '0', 10) || 0;
            if (fate < 0 || hero < 0 || fate + hero !== pool) {
                e.preventDefault();
                showFormError(form, `Suma dodatkowych punktów to ${fate + hero} — wymagane dokładnie ${pool}.`);
            }
        });
    });

    document.querySelectorAll('[data-validate="manual-100"]').forEach((form) => {
        form.addEventListener('submit', (e) => {
            clearFormError(form);
            const inputs = form.querySelectorAll('input[name^="manual_"]');
            let total = 0;
            for (const inp of inputs) {
                const v = parseInt(inp.value, 10);
                if (!Number.isFinite(v) || v < 4 || v > 18) {
                    e.preventDefault();
                    showFormError(form, 'Każda wartość musi być między 4 a 18.');
                    return;
                }
                total += v;
            }
            if (total !== 100) {
                e.preventDefault();
                showFormError(form, `Suma to ${total} — wymagane dokładnie 100 punktów.`);
            }
        });
    });

    document.querySelectorAll('[data-validate="origin-skills"]').forEach((form) => {
        form.addEventListener('submit', (e) => {
            clearFormError(form);
            const rows = Array.from(form.querySelectorAll('[data-skill-row]'));
            let plus3 = 0;
            let plus5 = 0;
            for (const row of rows) {
                const bonus = row.querySelector('input[type="radio"]:checked')?.value;
                if (bonus === '3') plus3++;
                if (bonus === '5') plus5++;
            }
            if (plus3 > 3) {
                e.preventDefault();
                showFormError(form, 'Możesz wybrać maksymalnie 3 umiejętności na +3.');
                return;
            }
            if (plus5 > 3) {
                e.preventDefault();
                showFormError(form, 'Możesz wybrać maksymalnie 3 umiejętności na +5.');
            }
        });
    });

    document.querySelectorAll('[data-validate="rearrange-rolls"]').forEach((form) => {
        form.addEventListener('submit', (e) => {
            clearFormError(form);
            const values = Array.from(form.querySelectorAll('input[name^="rollIndex_"]')).map(s => s.value);
            if (values.some(v => v === '')) {
                e.preventDefault();
                showFormError(form, 'Przypisz wszystkie rzuty do cech.');
                return;
            }
            if (new Set(values).size !== values.length) {
                e.preventDefault();
                showFormError(form, 'Każdy rzut może być przypisany tylko do jednej cechy.');
            }
        });
    });

    document.querySelectorAll('[data-live-sum]').forEach((el) => {
        const form = el.closest('form');
        if (!form) return;
        const target = parseInt(el.dataset.target || '0', 10);
        const selector = el.dataset.sumSelector || 'input[type="number"]';
        const update = () => {
            const total = sumInputs(form, selector);
            el.textContent = total;
            el.classList.toggle('text-red-400', total !== target);
            el.classList.toggle('text-emerald-400', total === target);
        };
        form.querySelectorAll(selector).forEach(inp => {
            inp.addEventListener('input', update);
            inp.addEventListener('change', update);
        });
        update();
    });
})();
