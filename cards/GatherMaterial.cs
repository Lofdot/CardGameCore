// Scripted by 0x4261756D
using CardGameCore;
using static CardGameUtils.GameConstants;
using static CardGameCore.CardUtils;

class GatherMaterial : Spell
{
	public GatherMaterial() : base(
		Name: "Gather Material",
		CardClass: PlayerClass.Artificer,
		OriginalCost: 1,
		Text: "{Cast}: [Gather] 6. If the gathered card is a creature with [Brittle] gain 1 Momentum.\n{Revelation}: If you control a creature with [Brittle], add this to your hand.",
		CanBeClassAbility: true
		)
	{ }

	public override void Init()
	{
		RegisterCastTrigger(trigger: new CastTrigger(effect: CastEffect), referrer: this);
		RegisterRevelationTrigger(trigger: new RevelationTrigger(effect: RevelationEffect, condition: RevelationCondition), referrer: this);
	}

	private bool RevelationCondition()
	{
		return ContainsValid(GetFieldUsed(Controller), Filter);
	}

	private bool Filter(Card card)
	{
		return card.Keywords.ContainsKey(Keyword.Brittle);
	}

	private void RevelationEffect()
	{
		MoveToHand(player: Controller, card: this);
	}

	private void CastEffect()
	{
		Card target = Gather(player: Controller, amount: 6);
		if(target.Keywords.ContainsKey(Keyword.Brittle))
		{
			PlayerChangeMomentum(player: Controller, amount: 1);
		}
	}
}