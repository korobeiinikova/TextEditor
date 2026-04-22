using System.IO;
using System.Windows.Forms;
using System;
using System.Numerics;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Xml.Linq;
using System.Data.Common;

namespace TextEditor
{
    public partial class Form1 : System.Windows.Forms.Form
    {
        public bool Changes = false;
        public string path;

        public Form1()
        {
            InitializeComponent();

            richTextBox1.TextChanged += richTextBox1_TextChanged;
            dataGridView3.EditingControlShowing += DataGridView3_EditingControlShowing;
            FormClosing += new FormClosingEventHandler(Form1_FormClosing);

        }
        private void DataGridView3_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (dataGridView3.CurrentCell.ColumnIndex == ColumnTypeSearh.Index)
            {
                ComboBox comboBox = e.Control as ComboBox;
                if (comboBox != null)
                {
                    comboBox.SelectedIndexChanged -= ComboBox_SelectedIndexChanged;
                    comboBox.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
                }
            }
        }

        private void ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            if (comboBox != null)
            {
                int rowIndex = dataGridView3.CurrentCell.RowIndex;
                if (rowIndex < 0) return;
                string selectedText = comboBox.Text;
                switch (selectedText)
                {
                    case "задание 1":
                        dataGridView3.Rows[rowIndex].Cells[ColumnTextSearh.Index].Value = "Все российские гласные буквы кроме а или А";
                        break;
                    case "задание 2":
                        dataGridView3.Rows[rowIndex].Cells[ColumnTextSearh.Index].Value = "Российские номера мобильных телефонов";
                        break;
                    case "задание 3":
                        dataGridView3.Rows[rowIndex].Cells[ColumnTextSearh.Index].Value = "IP-адрес (v4) с маской подсети";
                        break;
                    default:
                        dataGridView3.Rows[rowIndex].Cells[ColumnTextSearh.Index].Value = "";
                        break;
                }
                dataGridView3.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Changes)
            {
                DialogResult result = MessageBox.Show("Сохранить перед выходом?", "Выход", MessageBoxButtons.YesNoCancel);
                if (result == DialogResult.Yes)
                {
                    if (Changes)
                    {
                        Save();
                    }
                    e.Cancel = false;
                }
                else if (result == DialogResult.No)
                {
                    e.Cancel = false;
                }
                else e.Cancel = true;
            }
            else { e.Cancel = false; }
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            Changes = true;
        }

        private void выходToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Changes)
            {
                DialogResult result = MessageBox.Show("Сохранить изменения?", "Выход", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    Save();
                }
            }
            Close();
        }

        private void открытьToolStripMenuItem_Click(object sender, EventArgs e)
        {

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                if (Changes)
                {
                    DialogResult result = MessageBox.Show("Сохранить изменения в предыдущем файле?", "", MessageBoxButtons.YesNo);
                    if (result == DialogResult.Yes)
                    {
                        Save();
                    }
                }
                path = openFileDialog.FileName;
                richTextBox1.Text = File.ReadAllText(path);
            }
        }

        private void создатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (richTextBox1.Text.Length != 0)
            {
                DialogResult result = MessageBox.Show("Сохранить изменения в предыдущем файле?", "", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {
                    Save();
                }
            }
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Создание";
            if (saveFileDialog.ShowDialog() == DialogResult.Cancel)
                return;

            path = saveFileDialog.FileName;

            richTextBox1.Clear();
            Changes = false;
        }

        private void Save()
        {
            if (path is null)
            {
                MessageBox.Show("Файл не был создан. Чтобы не потерять изменения, создайте файл.");
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Title = "Создание";
                if (saveFileDialog.ShowDialog() == DialogResult.Cancel)
                    return;
                path = saveFileDialog.FileName;
            }
            FileInfo fileInfo = new FileInfo(path);
            File.WriteAllText(fileInfo.FullName, richTextBox1.Text);
            MessageBox.Show("Сохранено.");
            Changes = false;
        }

        private void сохранитьКакToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == DialogResult.Cancel)
                return;
            path = saveFileDialog.FileName;
            File.WriteAllText(path, richTextBox1.Text);
            MessageBox.Show("Файл сохранен");
        }

        private void сохранитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void отменитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (richTextBox1.CanUndo)
            {
                richTextBox1.Undo();
            }
            открытьToolStripMenuItem_Click(sender, e);
        }
        private void удалитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int start = richTextBox1.SelectionStart;
            int end = richTextBox1.SelectionLength;
            if (end > 0)
            {
                richTextBox1.Text = richTextBox1.Text.Remove(start, end);
                richTextBox1.SelectionStart = start;
            }
        }
        private void вырезатьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (richTextBox1.SelectedText != "")
                richTextBox1.Cut();
        }

        private void копироватьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (richTextBox1.SelectionLength > 0)
                richTextBox1.Copy();
        }

        private void вставитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Clipboard.GetDataObject().GetDataPresent(DataFormats.Text))
            {
                if (richTextBox1.SelectionLength > 0)
                {
                    if (MessageBox.Show("Do you want to paste over current selection?", "Cut Example", MessageBoxButtons.YesNo) == DialogResult.No)
                        richTextBox1.SelectionStart = richTextBox1.SelectionStart + richTextBox1.SelectionLength;
                }
                richTextBox1.Paste();
            }
        }

        private void повторитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (richTextBox1.CanRedo)
            {
                richTextBox1.Redo();
            }
        }

        private void выделитьВсёToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (richTextBox1.SelectionLength == 0)
                richTextBox1.SelectAll();

        }

        private void вызовСправкиToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string TextInfo = "Данная программа представляет собой текстовый редактор с графическим интерфейсом пользователя." +
                "\r\n\r\nОсновные возможности программы:\r\n\r\nМеню «Файл»:\r\n" +
                "- Создать - создание нового текстового файла.\r\n- Открыть - открытие существующего файла с диска.\r\n" +
                "- Сохранить - сохранение текущих изменений в файл.\r\n- Сохранить как - сохранение файла под новым именем." +
                "\r\n- Выход - завершение работы программы с предложением сохранить изменения.\r\n\r\nМеню «Правка»:\r\n" +
                "- Отменить - отмена последнего действия.\r\n- Повторить - повтор отменённого действия.\r\n" +
                "- Вырезать - удаление выделенного фрагмента текста с помещением его в буфер обмена.\r\n" +
                "- Копировать - копирование выделенного фрагмента текста в буфер обмена.\r\n- Вставить " +
                "- вставка текста из буфера обмена.\r\n- Выделить всё - выделение всего текста в редакторе.\r\n\r\n" +
                "Панель инструментов:\r\nСодержит кнопки быстрого доступа к основным функциям меню: создание," +
                " открытие и сохранение файла, отмена и повтор действий, операции с буфером обмена, вызов справки и " +
                "информации о программе.\r\n\r\nОбласть редактирования:\r\nВерхняя часть окна предназначена для ввода и " +
                "редактирования текста.\r\n\r\nОбласть вывода результатов:\r\nНижняя часть окна предназначена для отображения" +
                " результатов работы языкового процессора.\r\n\r\nМеню «Текст» и пункт «Пуск» будут реализованы на следующих этапах разработки.";
            MessageBox.Show(TextInfo, "Справка по работе с программой «Text editor»");
        }

        private void оПрограммеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string TextInfo = "Название: Text editor  \r\nТип программы: текстовый редактор" +
                "  \r\nНазначение: редактирование и сохранение текстовых файлов  \r\n\r\nПрограмма разработана" +
                " в рамках лабораторной работы\r\n«Разработка пользовательского интерфейса (GUI) для языкового процессора»." +
                "\r\n\r\nРедактор поддерживает основные операции работы с текстом и файлами,\r\nа также содержит область " +
                "для отображения результатов анализа текста,\r\nкоторая будет использоваться в дальнейших этапах разработки." +
                "\r\n\r\nЯзык программирования: C# \r\nПлатформа: Windows Forms  \r\n\r\nАвтор:\r\nСтудент(ка) группы АП-326\r\n" +
                "Коробейникова Дарья Романовна\r\n\r\nГод разработки: 2026";
            MessageBox.Show(TextInfo, "О программе");
        }

        public void пускToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            dataGridView2.Rows.Clear();
            richTextBox2.Clear();

            Lexer lexer = new Lexer(richTextBox1.Text);
            List<Token> tokens = lexer.analyze();

            foreach (Token token in tokens)
            {
                dataGridView1.Rows.Add(
                    token.code,
                    token.type,
                    token.token_name,
                    token.location.To_String()
                );
            }

            Parser parser = new Parser(tokens);
            parser.Parse();
            richTextBox2.Text = parser.AstText;
            dataGridView2.Rows.Add("", "", $"Количество ошибок: {parser.Errors.Count}");
            foreach (SyntaxError error in parser.Errors)
            {
                string fragment = error.Token?.token_name ?? "EOF";
                string location = error.Token != null
                    ? $"{error.Token.location.row} строка, позиция {error.Token.location.start}"
                    : "";

                dataGridView2.Rows.Add(
                    fragment,
                    location,
                    error.Message
                );
            }

            if (parser.Errors.Count != 0) tabControl1.SelectedTab = tabPage2;
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string locationText = dataGridView1.Rows[e.RowIndex].Cells[3].Value?.ToString();
            string fragment = dataGridView1.Rows[e.RowIndex].Cells[2].Value?.ToString();

            if (string.IsNullOrEmpty(locationText)) return;

            var parts = locationText
                .Replace("строка", "")
                .Split(',');

            if (parts.Length < 2) return;

            int row = int.Parse(parts[0].Trim());

            var positions = parts[1].Trim().Split('-');
            int start = int.Parse(positions[0]);
            int end = int.Parse(positions[1]);

            int index = GetIndexFromRowCol(row, start);
            int length = end - start + 1;

            richTextBox1.SelectionStart = index;
            richTextBox1.SelectionLength = length;
            richTextBox1.Focus();
        }

        private void dataGridView2_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex <= 0) return;

            string? locationText = dataGridView2.Rows[e.RowIndex].Cells[1].Value?.ToString();
            string? fragment = dataGridView2.Rows[e.RowIndex].Cells[0].Value?.ToString();

            if (string.IsNullOrWhiteSpace(locationText) || string.IsNullOrEmpty(fragment))
                return;

            var parts = locationText
                .Replace("строка", "")
                .Replace("позиция", "")
                .Split(',');

            int row = int.Parse(parts[0]);
            int col = int.Parse(parts[1]);

            int index = GetIndexFromRowCol(row, col);
            richTextBox1.SelectionStart = index;
            richTextBox1.SelectionLength = fragment.Length;
            richTextBox1.Focus();
        }

        private void dataGridView4_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex <= 0) return;
            if (dataGridView4.Rows[e.RowIndex].Cells[1].Value == null) return;
            string locationText = dataGridView4.Rows[e.RowIndex].Cells[1].Value.ToString();
            int fragmentLength = int.Parse(dataGridView4.Rows[e.RowIndex].Cells[2].Value.ToString());

            var parts = locationText.Split(',');

            int row = int.Parse(parts[0]);
            int col = int.Parse(parts[1]);

            int index = GetIndexFromRowCol(row, col);
            richTextBox1.SelectionStart = index;
            richTextBox1.SelectionLength = fragmentLength;
            richTextBox1.Focus();
        }

        private int GetIndexFromRowCol(int row, int col)
        {
            int index = 0;
            int currentRow = 1;

            foreach (char c in richTextBox1.Text)
            {
                if (currentRow == row)
                    break;

                index++;
                if (c == '\n')
                    currentRow++;
            }

            return index + col - 1;
        }

        private void dataGridView3_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            List<Searcher> searchers = new List<Searcher>();
            string row = "";
            string text = richTextBox1.Text;
            List<string> texts = new List<string>();
            foreach (char c in text)
            {
                if (c != '\n') row += c;
                else
                {
                    texts.Add(row);
                    row = "";
                }
            }
            if (!string.IsNullOrEmpty(row)) texts.Add(row);

            if (dataGridView3.Rows[0].Cells[0].Value == null)
            {
                MessageBox.Show("Выберите тип поиска");
            }
            Search search = new Search(searchers, texts);
            switch (dataGridView3.Rows[0].Cells[0].Value)
            {
                case "задание 1":
                    searchers = search.SearchLetters();
                    break;
                case "задание 2":
                    searchers = search.SearchPhoneNumber();
                    break;
                case "задание 3":
                    searchers = search.SearchIP();
                    break;
            }
            dataGridView4.Rows.Clear();
            foreach (Searcher s in searchers)
            {
                dataGridView4.Rows.Add(s.fragment, $"{s.location.row}, {s.location.start}", $"{s.location.end - s.location.start + 1}");
            }
        }
    }
}
