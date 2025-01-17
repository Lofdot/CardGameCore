// Scripted by 0x4261756D
using CardGameCore;
using static CardGameUtils.GameConstants;

class EverlastingProgress : Quest
{
	public EverlastingProgress() : base(
		Name: "Everlasting Progress",
		CardClass: PlayerClass.Artificer,
		ProgressGoal: 10,
		Text: "{Your creature with [Brittle] dies}: Gain 1 progress.\n{Reward}: All creatures with [Brittle] you control lose [Brittle] and gain +1/+2."
		)
	{ }

	public override void Init()
	{
		RegisterGenericDeathTrigger(trigger: new GenericDeathTrigger(effect: ProgressEffect, condition: ProgressCondition), referrer: this);
	}

	private bool ProgressCondition(Card destroyedCard)
	{
		return destroyedCard.Controller == Controller && destroyedCard.Keywords.ContainsKey(Keyword.Brittle);
	}

	private void ProgressEffect(Card destroyedCard)
	{
		Progress++;
	}

	public override void Reward()
	{
		RegisterLingeringEffect(info: new LingeringEffectInfo(effect: RewardEffect, referrer: this, influenceLocation: Location.Quest));
	}

	private void RewardEffect(Card target)
	{
		foreach(Card card in GetFieldUsed(Controller))
		{
			if(card.Keywords.Remove(Keyword.Brittle))
			{
				RegisterTemporaryLingeringEffect(new LingeringEffectInfo(effect: BuffEffect, referrer: card));
			}
		}
	}

	private void BuffEffect(Card target)
	{
		target.Life += 2;
		target.Power++;
	}
}