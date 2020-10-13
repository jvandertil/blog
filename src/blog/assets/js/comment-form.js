var highlightFields = function (response) {
    $('.form-field').removeClass('is-invalid');

    $.each(response, function (index, val) {
        var propName = val.memberNames[0];
        var nameSelector = '[name = "' + propName.replace(/(:|\.|\[|\])/g, "\\$1") + '"]',
            idSelector = '#' + propName.replace(/(:|\.|\[|\])/g, "\\$1");
        var $el = $(nameSelector) || $(idSelector);

        if (val.errorMessage.length > 0) {
            $el.addClass('is-invalid');
        }
    });
};

var highlightErrors = function (xhr) {
    try {
        var data = JSON.parse(xhr.responseText);
        highlightFields(data);
        //showSummary(data);
        //window.scrollTo(0, 0);
    } catch (e) {
    }
};

function showSuccessAlert() {
    $("#comment-success-alert").attr("aria-hidden", "false").removeClass("d-none");
}

function hideSuccessAlert() {
    $("#comment-success-alert").attr("aria-hidden", "true").addClass("d-none");
}

function showBusySpinner() {
    $("#submitSpinner").removeClass("d-none");
}

function hideBusySpinner() {
    $("#submitSpinner").addClass("d-none");
}

function handleSuccess(form) {
    $(form).find("input[type=text], textarea").val("");

    showSuccessAlert();
}

$('form[method=post]').not('.no-ajax').on('submit', function () {
    var submitBtn = $(this).find('[type="submit"]');

    submitBtn.prop('disabled', true);
    $(window).unbind();

    var $this = $(this);
    var formData = $this.serialize();

    $this.find('.is-invalid').removeClass('is-invalid');
    hideSuccessAlert();
    showBusySpinner();

    $.ajax({
        url: $this.attr('action'),
        type: 'post',
        data: formData,
        contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
        //dataType: 'json',
    }).fail(highlightErrors)
        .done(function () { handleSuccess($this); })
        .always(function () {
            submitBtn.prop('disabled', false);
            hideBusySpinner();
        })

    return false;
});