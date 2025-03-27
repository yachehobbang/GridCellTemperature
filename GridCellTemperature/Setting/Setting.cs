using UnityEngine;
using Verse;

namespace GridCellTemperature
{
	public abstract class Setting
	{
		protected readonly string Id;

		public abstract void ToDefault();

		public abstract void Scribe();

		protected Setting(string id) => this.Id = id;
	}

	public class Setting<T> : Setting where T : struct
	{
		private readonly T _default;
		public T Value;

		public Setting(string id, T defaultValue)
		  : base(id)
		{
			this._default = defaultValue;
			this.Value = defaultValue;
		}

		public override void ToDefault() => this.Value = this._default;

		public override void Scribe()
		{
			if (this.Value is float num1 && this._default is float num2 && Mathf.Approximately(num1, num2) || this.Value is Color color1 && this._default is Color color2 && GenColor.IndistinguishableFromFast(color1, color2))
				this.Value = this._default;
			Scribe_Values.Look<T>(ref this.Value, this.Id, this._default, false);
		}
	}
}
