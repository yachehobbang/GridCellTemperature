using System.Collections.Generic;
using System.Linq;

namespace RimWorld;

public class CompBook : CompReadable
{
	public new CompProperties_Book Props => (CompProperties_Book)props;

	public new IEnumerable<BookOutcomeDoer> Doers => doers.OfType<BookOutcomeDoer>();
}
