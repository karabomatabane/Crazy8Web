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

window.applyCardFanEffect = function () {
    const cards = document.querySelectorAll('.hand-container .fan-card');
    const totalCards = cards.length;
    const angle = 50; // total angle to fan out the cards
    const middleIndex = Math.floor(totalCards / 2);
    const step = angle / totalCards;

    cards.forEach((card, index) => {
        const rotation = -angle / 2 + step * index;
        card.style.transform = `translate(-50%, -50%) rotate(${rotation}deg)`;
    });
};

