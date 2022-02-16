using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lexvor.API.Objects.User {
	public class UserAccessory {
		public Guid Id { get; set; }
		public Guid OrderId { get; set; }
		public string Accessory { get; set; }
		public bool LifetimeWarranty { get; set; }
		public int LifetimeWarrantyPrice { get; set; }
		/// <summary>
		/// Price in cents
		/// </summary>
		public int Price { get; set; }
	}
}
