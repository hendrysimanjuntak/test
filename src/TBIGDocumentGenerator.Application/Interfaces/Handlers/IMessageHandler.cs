using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TBIGDocumentGenerator.Application.Interfaces.Handlers
{
	public interface IMessageHandler
	{
		Task HandleAsync(string message);
	}
}
