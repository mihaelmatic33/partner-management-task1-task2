(function ($) {
    function escapeHtml(value) {
        return $('<div />').text(value || '').html();
    }

    function renderDetails(data) {
        var address = data.address || '-';
        var croatianPin = data.croatianPin || '-';
        var policiesMarkup = data.policies.length === 0
            ? '<p class="mb-0 text-muted">Partner nema evidentiranih polica.</p>'
            : '<div class="table-responsive"><table class="table table-sm"><thead><tr><th>Broj police</th><th>Iznos</th><th>Kreirano</th></tr></thead><tbody>' +
                data.policies.map(function (policy) {
                    return '<tr><td>' + escapeHtml(policy.policyNumber) + '</td><td>' + escapeHtml(policy.policyAmount) + ' kn</td><td>' + escapeHtml(policy.createdAtUtc) + '</td></tr>';
                }).join('') +
              '</tbody></table></div>';

        return '' +
            '<dl class="row mb-4">' +
            '<dt class="col-sm-4">FullName</dt><dd class="col-sm-8">' + escapeHtml(data.fullName) + '</dd>' +
            '<dt class="col-sm-4">Address</dt><dd class="col-sm-8">' + escapeHtml(address) + '</dd>' +
            '<dt class="col-sm-4">PartnerNumber</dt><dd class="col-sm-8">' + escapeHtml(data.partnerNumber) + '</dd>' +
            '<dt class="col-sm-4">CroatianPIN</dt><dd class="col-sm-8">' + escapeHtml(croatianPin) + '</dd>' +
            '<dt class="col-sm-4">PartnerType</dt><dd class="col-sm-8">' + escapeHtml(data.partnerType) + '</dd>' +
            '<dt class="col-sm-4">CreatedAtUtc</dt><dd class="col-sm-8">' + escapeHtml(data.createdAtUtc) + '</dd>' +
            '<dt class="col-sm-4">CreatedByUser</dt><dd class="col-sm-8">' + escapeHtml(data.createdByUser) + '</dd>' +
            '<dt class="col-sm-4">IsForeign</dt><dd class="col-sm-8">' + (data.isForeign ? 'Da' : 'Ne') + '</dd>' +
            '<dt class="col-sm-4">ExternalCode</dt><dd class="col-sm-8">' + escapeHtml(data.externalCode) + '</dd>' +
            '<dt class="col-sm-4">Gender</dt><dd class="col-sm-8">' + escapeHtml(data.gender) + '</dd>' +
            '</dl>' +
            '<h6>Police</h6>' + policiesMarkup;
    }

    function clearPolicyErrors() {
        $('#policy-form-errors').addClass('d-none').empty();
    }

    function showPolicyErrors(errors) {
        var messages = [];
        Object.keys(errors).forEach(function (key) {
            errors[key].forEach(function (message) {
                messages.push('<div>' + escapeHtml(message) + '</div>');
            });
        });
        $('#policy-form-errors').removeClass('d-none').html(messages.join(''));
    }

    function updatePartnerRow(result) {
        var row = $('tr[data-partner-id="' + result.partnerId + '"]');
        row.find('.partner-full-name').text(result.fullName);
        row.find('.partner-policy-count').text(result.policyCount);
        row.find('.partner-total-amount').text(result.totalPolicyAmount + ' kn');
        row.addClass('table-warning font-weight-bold highlighted-row');
    }

    $(function () {
        $('#partners-table').on('click', '.partner-row', function (event) {
            if ($(event.target).closest('.open-policy-modal, a, button').length > 0) {
                return;
            }

            var detailsUrl = $(this).data('details-url');
            var $content = $('#partner-details-content');
            $content.html('<div class="text-center py-4 text-muted">Učitavanje...</div>');
            $('#partnerDetailsModal').modal('show');

            $.get(detailsUrl)
                .done(function (data) {
                    $content.html(renderDetails(data));
                })
                .fail(function () {
                    $content.html('<div class="alert alert-danger mb-0">Detalje nije moguće učitati.</div>');
                });
        });

        $('#partners-table').on('click', '.open-policy-modal', function () {
            clearPolicyErrors();
            $('#policy-partner-id').val($(this).data('partner-id'));
            $('#policy-partner-name').val($(this).data('partner-name'));
            $('#policy-number').val('');
            $('#policy-amount').val('');
            $('#policyCreateModal').modal('show');
        });

        $('#policy-form').on('submit', function (event) {
            event.preventDefault();
            clearPolicyErrors();

            $.ajax({
                url: $(this).attr('action'),
                method: 'POST',
                data: $(this).serialize(),
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            }).done(function (result) {
                if (!result.success) {
                    showPolicyErrors(result.errors);
                    return;
                }

                updatePartnerRow(result);
                $('#policyCreateModal').modal('hide');
            }).fail(function (xhr) {
                if (xhr.responseJSON && xhr.responseJSON.errors) {
                    showPolicyErrors(xhr.responseJSON.errors);
                    return;
                }

                showPolicyErrors({ general: ['Spremanje police nije uspjelo.'] });
            });
        });
    });
})(jQuery);