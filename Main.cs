using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace RapidFileSearch;

internal class Main : Form {
	private static readonly HashSet<string> result = [];

	private readonly Label Label1 = new(), Label2 = new(), LabelNum = new();
	private readonly TextBox TextTarget = new(), TextFilter = new();
	private readonly CheckBox CheckFull = new(), CheckRegex = new();
	private readonly Button ButtonSearch = new();
	private readonly DataGridView GridList = new();
	private readonly DataGridViewTextBoxColumn path = new(), length = new();

	private void InitializeComponent() {
		((ISupportInitialize)GridList).BeginInit();
		SuspendLayout();
		Label1.AutoSize = true;
		Label1.Location = new Point(13, 15);
		Label1.Name = "Label1";
		Label1.Size = new Size(55, 15);
		Label1.Text = "検索語句";
		Label2.AutoSize = true;
		Label2.Location = new Point(13, 44);
		Label2.Name = "Label2";
		Label2.Size = new Size(66, 15);
		Label2.Text = "フォルダ指定";
		LabelNum.AutoSize = true;
		LabelNum.Location = new Point(341, 44);
		LabelNum.Name = "LabelNum";
		LabelNum.Size = new Size(61, 15);
		LabelNum.Text = "検出 : 0 件";
		TextTarget.Location = new Point(85, 12);
		TextTarget.Name = "TextTarget";
		TextTarget.Size = new Size(250, 23);
		TextTarget.TabIndex = 0;
		TextTarget.KeyPress += new KeyPressEventHandler(TextTarget_KeyPress);
		TextFilter.Location = new Point(85, 41);
		TextFilter.Name = "TextFilter";
		TextFilter.Size = new Size(250, 23);
		TextFilter.TabIndex = 1;
		CheckFull.AutoSize = true;
		CheckFull.Location = new Point(341, 14);
		CheckFull.Name = "CheckFull";
		CheckFull.Size = new Size(98, 19);
		CheckFull.TabIndex = 2;
		CheckFull.Text = "フルパスにマッチ";
		CheckRegex.AutoSize = true;
		CheckRegex.Location = new Point(445, 14);
		CheckRegex.Name = "CheckRegex";
		CheckRegex.Size = new Size(74, 19);
		CheckRegex.TabIndex = 3;
		CheckRegex.Text = "正規表現";
		ButtonSearch.Location = new Point(525, 12);
		ButtonSearch.Name = "ButtonSearch";
		ButtonSearch.Size = new Size(39, 23);
		ButtonSearch.TabIndex = 4;
		ButtonSearch.Text = "検索";
		ButtonSearch.Click += new EventHandler(ButtonSearch_Click);
		GridList.AllowUserToAddRows = false;
		GridList.AllowUserToDeleteRows = false;
		GridList.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
		GridList.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
		GridList.Columns.AddRange([path, length]);
		GridList.Location = new Point(12, 70);
		GridList.MultiSelect = false;
		GridList.Name = "GridList";
		GridList.ReadOnly = true;
		GridList.RowHeadersVisible = false;
		GridList.RowTemplate.Height = 21;
		GridList.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
		GridList.Size = new Size(600, 442);
		GridList.TabIndex = 0;
		GridList.TabStop = false;
		GridList.CellDoubleClick += new DataGridViewCellEventHandler(GridList_CellDoubleClick);
		path.HeaderText = "path";
		path.Name = "path";
		path.ReadOnly = true;
		path.Width = 480;
		length.HeaderText = "length";
		length.Name = "length";
		length.ReadOnly = true;
		ClientSize = new Size(624, 524);
		Controls.Add(GridList);
		Controls.Add(ButtonSearch);
		Controls.Add(CheckRegex);
		Controls.Add(CheckFull);
		Controls.Add(TextFilter);
		Controls.Add(TextTarget);
		Controls.Add(LabelNum);
		Controls.Add(Label2);
		Controls.Add(Label1);
		FormBorderStyle = FormBorderStyle.FixedSingle;
		Name = "Main";
		((ISupportInitialize)(GridList)).EndInit();
		ResumeLayout(false);
		PerformLayout();
	}

	internal Main() => InitializeComponent();

	private void ButtonSearch_Click(object? sender, EventArgs e) {
		result.Clear();
		GridList.Rows.Clear();
		LabelNum.Text = "検出 : 0 件";
		if(!string.IsNullOrWhiteSpace(TextTarget.Text)) { _ = Task.Run(() => { if(CheckRegex.Checked) { FileSearchRegex(); } else { FileSearch(); } }); }
	}

	private void TextTarget_KeyPress(object? sender, KeyPressEventArgs e) { if(e.KeyChar == (char)Keys.Enter) ButtonSearch.PerformClick(); }

	private void GridList_CellDoubleClick(object? sender, DataGridViewCellEventArgs e) {
		if(e.RowIndex >= 0) {
			string? n = GridList.Rows[e.RowIndex].Cells[0].Value?.ToString();
			DirectoryInfo? d;
			if(n != null && File.Exists(n) && (d = Directory.GetParent(n)) != null) { _ = Process.Start(d.FullName); } else if(n != null && Directory.Exists(n)) { _ = Process.Start(n); }
		}
	}

	private void FileSearch() {
		if(string.IsNullOrWhiteSpace(TextFilter.Text)) {
			DriveInfo[] di = DriveInfo.GetDrives();
			_ = Parallel.For(0, di.Length, i => { if(di[i].IsReady) { Seek(di[i].RootDirectory.FullName, TextTarget.Text); } });
		} else {
			string[] filters = TextFilter.Text.Split('|', StringSplitOptions.RemoveEmptyEntries);
			_ = Parallel.For(0, filters.Length, i => Seek(filters[i], TextTarget.Text));
		}
	}

	private void FileSearchRegex() {
		if(string.IsNullOrWhiteSpace(TextFilter.Text)) {
			DriveInfo[] di = DriveInfo.GetDrives();
			_ = Parallel.For(0, di.Length, i => { if(di[i].IsReady) { Seek(di[i].RootDirectory.FullName, new Regex(TextTarget.Text)); } });
		} else {
			string[] filters = TextFilter.Text.Split('|', StringSplitOptions.RemoveEmptyEntries);
			_ = Parallel.For(0, filters.Length, i => Seek(filters[i], new Regex(TextTarget.Text)));
		}
	}

	private void Seek(string path, string target) {
		string[] dirs, files;
		try {
			files = Directory.GetFiles(path);
			dirs = Directory.GetDirectories(path);
		} catch { return; }
		if(files.Length > 0) {
			_ = Parallel.For(0, files.Length, i => {
				if((CheckFull.Checked ? files[i] : files[i].Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries)[^1]).Contains(target)) {
					lock(result) {
						_ = result.Add(files[i]);
						_ = Invoke(new MethodInvoker(() => {
							_ = GridList.Rows.Add(files[i], new FileInfo(files[i]).Length);
							//Folder folder = new Shell().NameSpace(path);
							//FolderItem item = folder.ParseName(filename);
							//GridList.Rows.Add(files[i], folder.GetDetailsOf(item, 27));
							LabelNum.Text = $"検出 : {result.Count} 件";
						}));
					}
				}
			});
		}
		if(dirs.Length > 0) {
			_ = Parallel.For(0, dirs.Length, i => {
				if((CheckFull.Checked ? dirs[i] : dirs[i].Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries)[^1]).Contains(target)) {
					lock(result) {
						_ = result.Add(dirs[i]);
						_ = Invoke(new MethodInvoker(() => {
							_ = GridList.Rows.Add(dirs[i], "");
							LabelNum.Text = $"検出 : {result.Count} 件";
						}));
					}
				}
				Seek(dirs[i], target);
			});
		}
	}

	private void Seek(string path, Regex target) {
		string[] dirs, files;
		try {
			files = Directory.GetFiles(path);
			dirs = Directory.GetDirectories(path);
		} catch { return; }
		if(files.Length > 0) {
			_ = Parallel.For(0, files.Length, i => {
				Match m = target.Match(CheckFull.Checked ? files[i] : files[i].Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries)[^1]);
				if(m.Success) {
					lock(result) {
						_ = result.Add(files[i]);
						_ = Invoke(new MethodInvoker(() => {
							_ = GridList.Rows.Add(files[i], new FileInfo(files[i]).Length);
							//Folder folder = new Shell().NameSpace(path);
							//FolderItem item = folder.ParseName(filename);
							//GridList.Rows.Add(files[i], folder.GetDetailsOf(item, 27));
							LabelNum.Text = $"検出 : {result.Count} 件";
						}));
					}
				}
			});
		}
		if(dirs.Length > 0) {
			_ = Parallel.For(0, dirs.Length, i => {
				Match m = target.Match(CheckFull.Checked ? dirs[i] : dirs[i].Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries)[^1]);
				if(m.Success) {
					lock(result) {
						_ = result.Add(dirs[i]);
						_ = Invoke(new MethodInvoker(() => {
							_ = GridList.Rows.Add(dirs[i], "");
							LabelNum.Text = $"検出 : {result.Count} 件";
						}));
					}
				}
				Seek(dirs[i], target);
			});
		}
	}
}