// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

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
    });
})(jQuery);
