namespace Lexvor.Eversign.Models.Response {
	public class Field {
		public int? Merge { get; set; }
		public string Identifier { get; set; }
		public string Name { get; set; }
		public object Options { get; set; }
		public string Group { get; set; }
		public string Value { get; set; }
		public string Type { get; set; }
		public double X { get; set; }
		public double Y { get; set; }
		public int? Page { get; set; }
		public int? Width { get; set; }
		public int? Height { get; set; }
		public string Signer { get; set; }
		public string ValidationType { get; set; }
		public int? Required { get; set; }
		public int? Readonly { get; set; }
		public object TextSize { get; set; }
		public string TextColor { get; set; }
		public string TextStyle { get; set; }
		public string TextFont { get; set; }
		public string Files { get; set; }
	}
}
