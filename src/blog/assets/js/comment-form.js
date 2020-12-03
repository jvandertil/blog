function isVisible(element) {
    return !element.classList.contains("d-none");
}

function hideElement(element) {
    element.classList.add("d-none");
    element.setAttribute("aria-hidden", "true")
}

function showElement(element) {
    element.classList.remove("d-none");
    element.setAttribute("aria-hidden", "false")
}

var highlightFields = function (response) {
    $('.form-field').removeClass('is-invalid');

    $.each(response, function (index, val) {
        var propName = val.memberName;
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
    var alertElement = document.getElementById('comment-success-alert');

    showElement(alertElement);
    document.getElementById('comment-success-alert').scrollIntoView({ behavior: 'smooth' });
}

function hideSuccessAlert() {
    var alertElement = document.getElementById('comment-success-alert');

    hideElement(alertElement);
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

    $this.find(".submitSpinner").removeClass("d-none");

    $.ajax({
        url: $this.attr('action'),
        type: 'post',
        data: formData,
        contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
    }).fail(highlightErrors)
        .done(function () { handleSuccess($this); })
        .always(function () {
            submitBtn.prop('disabled', false);
            $this.find(".submitSpinner").addClass("d-none");
        })

    return false;
});



function toggleForm(id) {
    var form = document.getElementById(id);
    if (isVisible(form)) {
        hideElement(form);
    } else {
        showElement(form);
    }
}
