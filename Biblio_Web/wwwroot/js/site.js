// Zie documentatie op https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// voor details over het configureren van bundling en minification van static web assets.

// Schrijf je JavaScript-code.

// AJAX helper (Nederlands) om late uitleningen gedeeltelijk te verversen
(function ($) {
    window.Biblio = window.Biblio || {};

    Biblio.refreshLateUitleningen = function (options) {
        options = options || {};
        var url = options.url || '/api/uitleningen/late';
        var container = options.container || '#late-uitleningen-wrapper';
        var $c = $(container);
        if (!$c.length) return;

        $c.html('<div class="text-center my-3"><div class="spinner-border" role="status"><span class="visually-hidden">Loading...</span></div></div>');

        $.ajax({
            url: url,
            method: 'GET',
            dataType: 'json'
        }).done(function (data) {
            if (!data || data.length === 0) {
                $c.html('<div class="alert alert-info">Geen achterstallige uitleningen.</div>');
                return;
            }

            var html = '<table class="table table-sm table-striped"><thead><tr><th>Titel</th><th>Lid</th><th>Start</th><th>Eind</th><th>Dagen te laat</th></tr></thead><tbody>';
            data.forEach(function (l) {
                var start = l.startDate ? new Date(l.startDate) : null;
                var due = l.dueDate ? new Date(l.dueDate) : null;
                var daysLate = due ? Math.max(0, Math.floor((Date.now() - due.getTime()) / (1000 * 60 * 60 * 24))) : 0;
                var lid = (l.lid && (l.lid.voornaam || l.lid.achterNaam)) ? ((l.lid.voornaam || '') + ' ' + (l.lid.achterNaam || '')) : '';
                html += '<tr>';
                html += '<td>' + (l.boek?.titel || '') + '</td>';
                html += '<td>' + lid + '</td>';
                html += '<td>' + (start ? start.toLocaleDateString() : '') + '</td>';
                html += '<td>' + (due ? due.toLocaleDateString() : '') + '</td>';
                html += '<td class="text-danger">' + daysLate + '</td>';
                html += '</tr>';
            });
            html += '</tbody></table>';
            $c.html(html);
        }).fail(function (xhr) {
            $c.html('<div class="alert alert-danger">Fout bij ophalen late uitleningen.</div>');
        });
    };

    // Auto-refresh als wrapper aanwezig is
    $(function () {
        if ($('#late-uitleningen-wrapper').length) {
            Biblio.refreshLateUitleningen();
            // optioneel: periodiek verversen
            // setInterval(function(){ Biblio.refreshLateUitleningen(); }, 60000);
        }

        // Zorg dat sidebar/offcanvas navigatie betrouwbaar werkt:
        // - Als offcanvas open is op kleine schermen, sluit deze eerst en navigeer daarna (betere UX).
        // - Als href niet is ingesteld of '#', laat standaardgedrag verder doorgaan.
        $(document).on('click', '#sidebar a.btn, .offcanvas-body a.btn', function (e) {
            try {
                var href = $(this).attr('href');
                if (!href || href === '#') return; // niets te doen

                var offcanvasEl = document.getElementById('offcanvasSidebar');
                if (offcanvasEl && offcanvasEl.classList.contains('show')) {
                    // Gebruik Bootstrap Offcanvas API om te verbergen, navigeer daarna na korte vertraging
                    var bsOff = bootstrap.Offcanvas.getInstance(offcanvasEl) || new bootstrap.Offcanvas(offcanvasEl);
                    bsOff.hide();
                    e.preventDefault();
                    // vertraging om de sluit-animatie af te ronden
                    setTimeout(function () {
                        window.location.href = href;
                    }, 220);
                }
                // Op desktop zorgt de anchor zelf voor navigatie
            } catch (ex) {
                // Als bootstrap niet beschikbaar is of bij andere fouten, val terug op standaardnavigatie
                console.warn('Fout in navigatiehelper', ex);
            }
        });
    });
})(jQuery);
