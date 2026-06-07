(function () {
    document.querySelectorAll('[data-tabs]').forEach((root) => {
        const buttons = root.querySelectorAll('[data-tab-target]');
        const panels = root.querySelectorAll('[data-tab-panel]');

        function activate(id) {
            buttons.forEach((btn) => {
                const active = btn.dataset.tabTarget === id;
                btn.classList.toggle('border-amber-500', active);
                btn.classList.toggle('text-amber-400', active);
                btn.classList.toggle('border-transparent', !active);
                btn.classList.toggle('text-stone-400', !active);
                btn.setAttribute('aria-selected', active ? 'true' : 'false');
            });
            panels.forEach((panel) => {
                panel.classList.toggle('hidden', panel.dataset.tabPanel !== id);
            });
        }

        buttons.forEach((btn) => {
            btn.addEventListener('click', () => activate(btn.dataset.tabTarget));
        });

        const initial = root.dataset.tabsDefault || buttons[0]?.dataset.tabTarget;
        if (initial) activate(initial);
    });
})();
