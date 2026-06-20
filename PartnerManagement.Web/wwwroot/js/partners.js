(function ($) {
    var state = {
        offset: 0,
        limit: 10,
        isLoading: false,
        hasMore: true,
        requestNonce: 0,
        highlightId: null,
        highlightApplied: false,
        listUrl: '',
        policyCreateUrl: '',
        policyEditUrlTemplate: '',
        activeDetailsUrl: null,
        lastPartnerId: null,
        lastPartnerName: '',
        isPolicySubmitting: false,
        policyPartnerLookupNonce: 0
    };

    function escapeHtml(value) {
        return $('<div />').text(value || '').html();
    }

    function toNullableText(value) {
        if (value === undefined || value === null) {
            return null;
        }

        var trimmed = String(value).trim();
        return trimmed.length === 0 ? null : trimmed;
    }

    function toNullableNumber(value) {
        if (value === undefined || value === null || value === '') {
            return null;
        }

        var numeric = Number(value);
        return Number.isFinite(numeric) ? numeric : null;
    }

    function readQuery() {
        return {
            offset: state.offset,
            limit: state.limit,
            search: toNullableText($('#partners-search').val()),
            name: toNullableText($('#partners-name-filter').val()),
            croatianPin: toNullableText($('#partners-pin-filter').val()),
            createdFrom: toNullableText($('#partners-created-from').val()),
            createdTo: toNullableText($('#partners-created-to').val()),
            minPolicyAmount: toNullableNumber($('#partners-amount-min').val()),
            maxPolicyAmount: toNullableNumber($('#partners-amount-max').val()),
            onlyActive: $('#partners-only-active').is(':checked') ? true : null
        };
    }

    function showInlineSuccess(message) {
        var $alert = $('#partners-inline-success');
        $alert.text(message).removeClass('d-none');

        setTimeout(function () {
            $alert.addClass('d-none').text('');
        }, 2600);
    }

    function getRowClass(partnerId) {
        if (state.highlightApplied || state.highlightId === null || state.highlightId !== partnerId) {
            return '';
        }

        state.highlightApplied = true;
        return 'table-warning font-weight-bold highlighted-row';
    }

    function buildPartnerRow(item) {
        var rowClass = getRowClass(item.id);
        var croatianPin = item.croatianPin ? escapeHtml(item.croatianPin) : '-';

        return '' +
            '<tr class="partner-row ' + rowClass + '" data-partner-id="' + item.id + '" data-details-url="' + escapeHtml(item.detailsUrl) + '">' +
                '<td class="partner-full-name">' + escapeHtml(item.fullName) + '</td>' +
                '<td>' + escapeHtml(item.partnerNumber) + '</td>' +
                '<td>' + croatianPin + '</td>' +
                '<td>' + escapeHtml(item.partnerType) + '</td>' +
                '<td>' + escapeHtml(item.createdAtUtc) + '</td>' +
                '<td>' + (item.isForeign ? 'Da' : 'Ne') + '</td>' +
                '<td>' + (item.isActive ? 'Da' : 'Ne') + '</td>' +
                '<td>' + escapeHtml(item.gender) + '</td>' +
                '<td class="partner-policy-count">' + escapeHtml(item.policyCount) + '</td>' +
                '<td class="partner-total-amount">' + escapeHtml(item.totalPolicyAmount) + ' kn</td>' +
                '<td class="text-right">' +
                    '<a class="btn btn-outline-dark btn-sm" href="' + escapeHtml(item.editUrl) + '" title="Uredi partnera" aria-label="Uredi partnera"><span aria-hidden="true">&#9998;</span></a> ' +
                    '<button type="button" class="btn btn-outline-primary btn-sm open-policy-modal" data-partner-id="' + item.id + '" data-partner-name="' + escapeHtml(item.fullName) + '">Unos police</button> ' +
                    '<a class="btn btn-outline-secondary btn-sm" href="' + escapeHtml(item.policyFormUrl) + '">Forma police</a>' +
                '</td>' +
            '</tr>';
    }

    function renderRows(items, replace) {
        var markup = items.map(buildPartnerRow).join('');

        if (replace) {
            $('#partners-table tbody').html(markup);
            return;
        }

        $('#partners-table tbody').append(markup);
    }

    function setLoading(isLoading) {
        $('#partners-loading').toggleClass('d-none', !isLoading);
    }

    function setEmpty(isEmpty) {
        $('#partners-empty').toggleClass('d-none', !isEmpty);
    }

    function setEnd(isVisible) {
        $('#partners-end').toggleClass('d-none', !isVisible);
    }

    function computeFirstPageThreshold() {
        var $rows = $('#partners-table tbody .partner-row');
        if ($rows.length === 0) {
            return Number.POSITIVE_INFINITY;
        }

        if ($rows.length < 10) {
            return Number.POSITIVE_INFINITY;
        }

        var $tenthRow = $rows.eq(9);
        var threshold = $tenthRow.offset().top - 120;
        return Number.isFinite(threshold) ? threshold : Number.POSITIVE_INFINITY;
    }

    function syncScrollActionsVisibility() {
        var $footer = $('#app-footer');
        var $floatingCta = $('#floating-policy-cta');
        var threshold = computeFirstPageThreshold();
        var shouldShow = Number.isFinite(threshold) && $(window).scrollTop() >= threshold;

        $footer.toggleClass('floating-active', shouldShow);
        $floatingCta.toggleClass('d-none', !shouldShow);
    }

    function buildPolicyEditUrl(policyId) {
        return state.policyEditUrlTemplate.replace('__POLICY_ID__', policyId);
    }

    function openPolicyCreateModal(partnerId, partnerName, usePartnerPicker) {
        var partnerPickerMode = !!usePartnerPicker;
        clearPolicyErrors();
        $('#policy-partner-picker-group').toggleClass('d-none', !partnerPickerMode);
        $('#policy-partner-static-group').toggleClass('d-none', partnerPickerMode);
        $('#policy-id').val('');
        $('#policy-partner-id').val(partnerPickerMode ? '' : partnerId);
        $('#policy-partner-name').val(partnerPickerMode ? '' : partnerName);

        if (partnerPickerMode) {
            $('#policy-partner-search').val('');
            $('#policy-partner-select').empty();
            loadPartnerOptions('');
        }

        $('#policy-number').val('');
        $('#policy-amount').val('');
        $('#policy-form').attr('action', state.policyCreateUrl);
        $('#policyCreateModalLabel').text('Unos police');
        $('#policyCreateModal button[type="submit"]').text('Spremi policu');
        $('#policyCreateModal').modal('show');
    }

    function openPolicyEditModal(policyId, partnerId, partnerName, policyNumber, policyAmount) {
        clearPolicyErrors();
        $('#policy-id').val(policyId);
        $('#policy-partner-id').val(partnerId);
        $('#policy-partner-name').val(partnerName);
        $('#policy-number').val(policyNumber);
        $('#policy-amount').val(policyAmount);
        $('#policy-form').attr('action', buildPolicyEditUrl(policyId));
        $('#policyCreateModalLabel').text('Izmjena police');
        $('#policyCreateModal button[type="submit"]').text('Spremi izmjene police');
        $('#policyCreateModal').modal('show');
    }

    function openDetailsModal(detailsUrl) {
        var $content = $('#partner-details-content');
        state.activeDetailsUrl = detailsUrl;
        $content.html('<div class="text-center py-4 text-muted">Učitavanje...</div>');
        $('#partnerDetailsModal').modal('show');

        $.get(detailsUrl)
            .done(function (data) {
                state.lastPartnerId = data.id;
                state.lastPartnerName = data.fullName;
                $content.html(renderDetails(data));
            })
            .fail(function () {
                $content.html('<div class="alert alert-danger mb-0">Detalje nije moguće učitati.</div>');
            });
    }

    function refreshActiveDetails() {
        if (!state.activeDetailsUrl || !$('#partnerDetailsModal').hasClass('show')) {
            return;
        }

        $.get(state.activeDetailsUrl)
            .done(function (data) {
                state.lastPartnerId = data.id;
                state.lastPartnerName = data.fullName;
                $('#partner-details-content').html(renderDetails(data));
            });
    }

    function loadNextBatch(replaceRows) {
        if (state.isLoading || !state.hasMore) {
            return;
        }

        state.isLoading = true;
        setLoading(true);
        setEnd(false);

        var nonce = ++state.requestNonce;
        var query = readQuery();

        $.get(state.listUrl, query)
            .done(function (result) {
                if (nonce !== state.requestNonce) {
                    return;
                }

                var items = result.items || [];

                renderRows(items, replaceRows);
                state.offset += items.length;
                state.hasMore = !!result.hasMore;

                if (replaceRows) {
                    setEmpty(items.length === 0);
                }

                if (!state.hasMore) {
                    setEnd(items.length > 0);
                }
            })
            .fail(function () {
                if (replaceRows) {
                    $('#partners-table tbody').empty();
                    setEmpty(true);
                }
            })
            .always(function () {
                if (nonce !== state.requestNonce) {
                    return;
                }

                state.isLoading = false;
                setLoading(false);
                ensureViewportFilled();
                syncScrollActionsVisibility();
            });
    }

    function resetAndReload() {
        state.offset = 0;
        state.hasMore = true;
        state.highlightApplied = false;
        setEmpty(false);
        setEnd(false);
        loadNextBatch(true);
    }

    function maybeLoadByScroll() {
        if (state.isLoading || !state.hasMore) {
            return;
        }

        var distanceToBottom = $(document).height() - ($(window).scrollTop() + $(window).height());
        if (distanceToBottom < 180) {
            loadNextBatch(false);
        }
    }

    function ensureViewportFilled() {
        if (state.isLoading || !state.hasMore) {
            return;
        }

        if ($(document).height() <= $(window).height() + 40) {
            loadNextBatch(false);
        }
    }

    function debounce(fn, delayMs) {
        var timer = null;

        return function () {
            var args = arguments;
            clearTimeout(timer);
            timer = setTimeout(function () {
                fn.apply(null, args);
            }, delayMs);
        };
    }

    function renderDetails(data) {
        var address = data.address || '-';
        var croatianPin = data.croatianPin || '-';
        var detailsActions = '<div class="mb-3 text-right"><button type="button" class="btn btn-primary open-policy-from-details" data-partner-id="' + data.id + '" data-partner-name="' + escapeHtml(data.fullName) + '">Dodaj policu</button></div>';
        var policiesMarkup = data.policies.length === 0
            ? '<p class="mb-0 text-muted">Partner nema evidentiranih polica.</p>'
            : '<div class="table-responsive"><table class="table table-sm"><thead><tr><th>Broj police</th><th>Iznos</th><th>Kreirano</th><th class="text-right">Akcija</th></tr></thead><tbody>' +
                data.policies.map(function (policy) {
                    return '<tr><td>' + escapeHtml(policy.policyNumber) + '</td><td>' + escapeHtml(policy.policyAmount) + ' kn</td><td>' + escapeHtml(policy.createdAtUtc) + '</td><td class="text-right"><button type="button" class="btn btn-sm btn-outline-primary edit-policy-inline" data-policy-id="' + policy.id + '" data-policy-number="' + escapeHtml(policy.policyNumber) + '" data-policy-amount="' + escapeHtml(policy.policyAmount) + '">Izmijeni</button></td></tr>';
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
            '<dt class="col-sm-4">Aktivan</dt><dd class="col-sm-8">' + (data.isActive ? 'Da' : 'Ne') + '</dd>' +
            '<dt class="col-sm-4">ExternalCode</dt><dd class="col-sm-8">' + escapeHtml(data.externalCode) + '</dd>' +
            '<dt class="col-sm-4">Gender</dt><dd class="col-sm-8">' + escapeHtml(data.gender) + '</dd>' +
            '</dl>' +
                detailsActions +
            '<h6>Police</h6>' + policiesMarkup;
    }

    function clearPolicyErrors() {
        $('#policy-form-errors').addClass('d-none').empty();
        $('#policy-partner-error').text('');
        $('#policy-number-error').text('');
        $('#policy-amount-error').text('');
        $('#policy-partner-select').removeClass('is-invalid');
        $('#policy-number').removeClass('is-invalid');
        $('#policy-amount').removeClass('is-invalid');
    }

    function setFieldError(fieldId, errorId, message) {
        $(fieldId).addClass('is-invalid');
        $(errorId).text(message);
    }

    function isRequiredLikeMessage(message) {
        var normalized = String(message || '').toLowerCase();
        return normalized.indexOf('required') >= 0
            || normalized.indexOf('obavezno') >= 0
            || normalized.indexOf('is invalid') >= 0
            || normalized.indexOf('nije ispravno') >= 0;
    }

    function showPolicyErrors(errors) {
        var messages = [];
        Object.keys(errors).forEach(function (key) {
            errors[key].forEach(function (message) {
                var effectiveMessage = isRequiredLikeMessage(message) ? 'Ovo polje je obavezno.' : message;

                if (key === 'PartnerId' || key === 'PolicyForm.PartnerId') {
                    setFieldError('#policy-partner-select', '#policy-partner-error', effectiveMessage);
                }

                if (key === 'PolicyNumber' || key === 'PolicyForm.PolicyNumber') {
                    setFieldError('#policy-number', '#policy-number-error', effectiveMessage);
                }

                if (key === 'PolicyAmount' || key === 'PolicyForm.PolicyAmount') {
                    setFieldError('#policy-amount', '#policy-amount-error', effectiveMessage);
                }

                messages.push('<div>' + escapeHtml(message) + '</div>');
            });
        });
        $('#policy-form-errors').removeClass('d-none').html(messages.join(''));
    }

    function validatePolicyFormBeforeSubmit() {
        var isValid = true;
        var partnerId = String($('#policy-partner-id').val() || '').trim();
        var policyNumber = String($('#policy-number').val() || '').trim();
        var policyAmount = String($('#policy-amount').val() || '').trim();

        if (partnerId.length === 0) {
            setFieldError('#policy-partner-select', '#policy-partner-error', 'Ovo polje je obavezno.');
            isValid = false;
        }

        if (policyNumber.length === 0) {
            setFieldError('#policy-number', '#policy-number-error', 'Ovo polje je obavezno.');
            isValid = false;
        }

        if (policyAmount.length === 0) {
            setFieldError('#policy-amount', '#policy-amount-error', 'Ovo polje je obavezno.');
            isValid = false;
        }

        return isValid;
    }

    function renderPartnerOptions(items) {
        var optionsMarkup = items.map(function (item, index) {
            var selected = index === 0 ? ' selected' : '';
            return '<option value="' + item.id + '"' + selected + '>' + escapeHtml(item.fullName) + '</option>';
        }).join('');

        $('#policy-partner-select').html(optionsMarkup);

        if (items.length === 0) {
            $('#policy-partner-id').val('');
            return;
        }

        $('#policy-partner-id').val(items[0].id);
        $('#policy-partner-name').val(items[0].fullName);
    }

    function loadPartnerOptions(searchText) {
        var nonce = ++state.policyPartnerLookupNonce;

        $.get(state.listUrl, {
            offset: 0,
            limit: 25,
            search: toNullableText(searchText)
        }).done(function (result) {
            if (nonce !== state.policyPartnerLookupNonce) {
                return;
            }

            renderPartnerOptions(result.items || []);
        }).fail(function () {
            if (nonce !== state.policyPartnerLookupNonce) {
                return;
            }

            $('#policy-partner-select').empty();
            $('#policy-partner-id').val('');
            $('#policy-partner-error').text('Partnere trenutno nije moguće učitati.');
            $('#policy-partner-select').addClass('is-invalid');
        });
    }

    function updatePartnerRow(result) {
        var row = $('tr[data-partner-id="' + result.partnerId + '"]');
        if (row.length === 0) {
            return;
        }

        row.find('.partner-full-name').text(result.fullName);
        row.find('.partner-policy-count').text(result.policyCount);
        row.find('.partner-total-amount').text(result.totalPolicyAmount + ' kn');
        row.addClass('table-warning font-weight-bold highlighted-row');
    }

    $(function () {
        var $root = $('#partners-list-root');
        state.listUrl = $root.data('list-url');
        state.policyCreateUrl = $root.data('policy-create-url');
        state.policyEditUrlTemplate = $root.data('policy-edit-url-template');

        var highlightRaw = $root.data('highlight-id');
        if (highlightRaw !== undefined && highlightRaw !== null && String(highlightRaw).length > 0) {
            state.highlightId = Number(highlightRaw);
            if (!Number.isFinite(state.highlightId)) {
                state.highlightId = null;
            }
        }

        var debouncedSearch = debounce(function () {
            resetAndReload();
        }, 300);

        $('#partners-filter-form').on('submit', function (event) {
            event.preventDefault();
            resetAndReload();
        });

        $('#partners-reset-filters').on('click', function () {
            $('#partners-filter-form')[0].reset();
            resetAndReload();
        });

        $('#partners-search').on('input', debouncedSearch);

        $(window).on('scroll', maybeLoadByScroll);
        $(window).on('scroll resize', syncScrollActionsVisibility);

        resetAndReload();
        syncScrollActionsVisibility();

        $('#floating-policy-cta').on('click', function () {
            openPolicyCreateModal(null, '', true);
        });

        $('#partners-table').on('click', '.partner-row', function (event) {
            if ($(event.target).closest('.open-policy-modal, a, button').length > 0) {
                return;
            }

            var detailsUrl = $(this).data('details-url');
            openDetailsModal(detailsUrl);
        });

        $('#partners-table').on('click', '.open-policy-modal', function () {
            var partnerId = Number($(this).data('partner-id'));
            var partnerName = $(this).data('partner-name');
            state.lastPartnerId = partnerId;
            state.lastPartnerName = partnerName;
            openPolicyCreateModal(partnerId, partnerName, false);
        });

        $('#partner-details-content').on('click', '.open-policy-from-details', function () {
            var partnerId = Number($(this).data('partner-id'));
            var partnerName = $(this).data('partner-name');
            state.lastPartnerId = partnerId;
            state.lastPartnerName = partnerName;
            openPolicyCreateModal(partnerId, partnerName, false);
        });

        $('#policy-partner-search').on('input', debounce(function () {
            loadPartnerOptions($('#policy-partner-search').val());
        }, 250));

        $('#policy-partner-select').on('change', function () {
            var selectedOption = $(this).find('option:selected');
            var partnerId = selectedOption.val();
            var partnerName = selectedOption.text();

            $('#policy-partner-id').val(partnerId || '');
            $('#policy-partner-name').val(partnerName || '');
            $('#policy-partner-error').text('');
            $('#policy-partner-select').removeClass('is-invalid');
        });

        $('#policyCreateModal').on('hidden.bs.modal', function () {
            $('#policy-partner-picker-group').addClass('d-none');
            $('#policy-partner-static-group').removeClass('d-none');
        });

        $('#partner-details-content').on('click', '.edit-policy-inline', function () {
            if (state.lastPartnerId === null) {
                return;
            }

            var policyId = Number($(this).data('policy-id'));
            if (!Number.isFinite(policyId)) {
                return;
            }

            var policyNumber = $(this).data('policy-number');
            var policyAmount = $(this).data('policy-amount');
            openPolicyEditModal(policyId, state.lastPartnerId, state.lastPartnerName, policyNumber, policyAmount);
        });

        $('#policy-form').on('submit', function (event) {
            event.preventDefault();

            if (state.isPolicySubmitting) {
                return;
            }

            clearPolicyErrors();

            if (!validatePolicyFormBeforeSubmit()) {
                return;
            }

            state.isPolicySubmitting = true;
            var $submitButton = $('#policyCreateModal button[type="submit"]');
            $submitButton.prop('disabled', true).addClass('disabled');

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
                refreshActiveDetails();
                showInlineSuccess('Polica je uspješno spremljena.');
            }).fail(function (xhr) {
                if (xhr.responseJSON && xhr.responseJSON.errors) {
                    showPolicyErrors(xhr.responseJSON.errors);
                    return;
                }

                showPolicyErrors({ general: ['Spremanje police nije uspjelo.'] });
            }).always(function () {
                state.isPolicySubmitting = false;
                $submitButton.prop('disabled', false).removeClass('disabled');
            });
        });
    });
})(jQuery);