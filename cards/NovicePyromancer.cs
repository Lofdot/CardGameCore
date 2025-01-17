// Scripted by 0x4261756D
using CardGameCore;
using static CardGameUtils.GameConstants;
using static CardGameCore.CardUtils;

class NovicePyromancer : Creature
{
	public NovicePyromancer() : base(
		Name: "Novice Pyromancer",
		CardClass: PlayerClass.Pyromancer,
		OriginalCost: 3,
		Text: "{A creature dies}: Deal 1 damage any target.",
		OriginalLife: 4,
		OriginalPower: 2
		)
	{ }

	public override void Init()
	{
		RegisterGenericDeathTrigger(trigger: new GenericDeathTrigger(effect: DamageEffect), referrer: this);
	}

	private void DamageEffect(Card destroyedCard)
	{
		ChangeLifeOfAnyTarget(player: Controller, amount: -1, description: "Damage", source: this);
	}
}