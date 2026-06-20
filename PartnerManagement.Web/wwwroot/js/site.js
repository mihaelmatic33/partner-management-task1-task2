(function () {
	var alert = document.querySelector('.alert-success');
	if (!alert) {
		return;
	}

	window.setTimeout(function () {
		alert.classList.add('fade');
	}, 3200);
})();
