(function () {
	var alert = document.querySelector('.alert-success');
	if (!alert) {
		return;
	}

	window.setTimeout(function () {
		alert.classList.add('fade');
	}, 3200);
})();

(function () {
	var forms = document.querySelectorAll('form.js-prevent-double-submit');
	if (!forms.length) {
		return;
	}

	forms.forEach(function (form) {
		form.addEventListener('submit', function () {
			if (!form.checkValidity()) {
				return;
			}

			var submitButtons = form.querySelectorAll('button[type="submit"], input[type="submit"]');
			submitButtons.forEach(function (button) {
				button.setAttribute('disabled', 'disabled');
				button.classList.add('disabled');
			});
		});
	});
})();
