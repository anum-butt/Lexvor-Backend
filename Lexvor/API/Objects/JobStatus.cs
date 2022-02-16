using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Objects {

	public enum Status { Faild = 0, Running = 1, Pending = 2, Complete = 3} 

	public class JobStatus {

		public int Id { get; set; }
		public string Job { get; set; }
		public byte Status { get; set; }
		public DateTime RunTime { get; set; }
		public string Message { get; set; }
	}
}
