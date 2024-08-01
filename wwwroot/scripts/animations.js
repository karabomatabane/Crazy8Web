function animateCard(cardIndex) {

    const card = document.querySelector(".selected");

    if (!card) {
        return;
    }
    card.classList.add("animate");
    card.addEventListener('animationend', function () {
        card.classList.remove("animate");
        card.style.display = "none";
    }, {once: true});
}
