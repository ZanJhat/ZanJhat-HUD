using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Engine;
using XmlUtilities;
using Game;
using ZanJhat.Core;

namespace ZanJhat.HUD
{
    public class RecipaediaIngredientUsageScreen : Screen
    {
        public ListPanelWidget m_recipesList;
        public LabelWidget m_categoryLabel;
        public Screen m_previousScreen;

        public int m_ingredientValue;
        public const string fName = "RecipaediaIngredientUsageScreen";

        public RecipaediaIngredientUsageScreen()
        {
            XElement node = ContentManager.Get<XElement>("Screens/RecipaediaScreen");
            LoadContents(this, node);

            m_recipesList = Children.Find<ListPanelWidget>("BlocksList");
            m_categoryLabel = Children.Find<LabelWidget>("Category");

            // Ẩn các nút chuyển danh mục vì chúng ta không cần phân loại ở màn hình này
            Children.Find<ButtonWidget>("PreviousCategory").IsVisible = false;
            Children.Find<ButtonWidget>("NextCategory").IsVisible = false;
            Children.Find<ButtonWidget>("DetailsButton").IsVisible = false;
            Children.Find<ButtonWidget>("RecipesButton").IsVisible = false;

            m_recipesList.ItemWidgetFactory = delegate (object item)
            {
                // Item trong danh sách lúc này là đối tượng CraftingRecipe
                CraftingRecipe recipe = (CraftingRecipe)item;
                int resultValue = recipe.ResultValue;
                int contents = Terrain.ExtractContents(resultValue);
                Block block = BlocksManager.Blocks[contents];

                XElement node2 = ContentManager.Get<XElement>("Widgets/RecipaediaItem");
                ContainerWidget obj = (ContainerWidget)LoadWidget(this, node2, null);

                // Hiển thị icon và tên của vật phẩm ĐƯỢC TẠO RA (Result)
                obj.Children.Find<BlockIconWidget>("RecipaediaItem.Icon").Value = resultValue;
                obj.Children.Find<LabelWidget>("RecipaediaItem.Text").Text = block.GetDisplayName(null, resultValue);

                // Hiển thị số lượng tạo ra và mô tả
                string description = $"Output: {recipe.ResultCount} | {block.GetDescription(resultValue).Replace("\n", "  ")}";
                obj.Children.Find<LabelWidget>("RecipaediaItem.Details").Text = description;

                return obj;
            };

            m_recipesList.ItemClicked += OnRecipesListItemClicked;
        }

        // Xử lý sự kiện khi click vào một công thức
        public virtual void OnRecipesListItemClicked(object item)
        {
            if (m_recipesList.SelectedItem == item && item is CraftingRecipe)
            {
                CraftingRecipe recipe = (CraftingRecipe)item;
                int resultValue = recipe.ResultValue;
                Block block = BlocksManager.Blocks[Terrain.ExtractContents(resultValue)];

                // Mở màn hình xem công thức chế tạo của vật phẩm đầu ra
                ScreensManager.m_screens["RecipaediaRecipes"] = block.GetBlockRecipeScreen(resultValue);
                ScreensManager.SwitchScreen("RecipaediaRecipes", resultValue);
            }
        }

        public override void Enter(object[] parameters)
        {
            if (ScreensManager.PreviousScreen != ScreensManager.FindScreen<Screen>("RecipaediaRecipes") &&
                ScreensManager.PreviousScreen != ScreensManager.FindScreen<Screen>("RecipaediaDescription"))
            {
                m_previousScreen = ScreensManager.PreviousScreen;
            }

            // Nhận value của block truyền vào từ phương thức SwitchScreen
            if (parameters.Length > 0 && parameters[0] is int)
            {
                m_ingredientValue = (int)parameters[0];
                PopulateRecipesList();
            }
        }

        public override void Update()
        {
            if (Input.Back || Input.Cancel || Children.Find<ButtonWidget>("TopBar.Back").IsClicked)
            {
                ScreensManager.SwitchScreen(m_previousScreen);
            }
        }

        // Tìm và nạp danh sách công thức
        public void PopulateRecipesList()
        {
            m_recipesList.ScrollPosition = 0f;
            m_recipesList.ClearItems();

            Block ingredientBlock = BlocksManager.Blocks[Terrain.ExtractContents(m_ingredientValue)];
            string targetCraftingId = ingredientBlock.CraftingId;
            int targetData = Terrain.ExtractData(m_ingredientValue);

            List<CraftingRecipe> matchingRecipes = new List<CraftingRecipe>();

            // Quét toàn bộ công thức trong game
            foreach (CraftingRecipe recipe in CraftingRecipesManager.Recipes)
            {
                bool isUsed = false;

                // Kiểm tra từng ô nguyên liệu (Ingredients)
                foreach (string ingredientStr in recipe.Ingredients)
                {
                    if (string.IsNullOrEmpty(ingredientStr)) continue;

                    // Decode chuỗi nguyên liệu theo hàm có sẵn của hệ thống
                    CraftingRecipesManager.DecodeIngredient(ingredientStr, out string reqId, out int? reqData);

                    // Nếu CraftingId khớp và Data khớp (hoặc công thức không yêu cầu Data cụ thể)
                    if (reqId == targetCraftingId)
                    {
                        if (!reqData.HasValue || reqData.Value == targetData)
                        {
                            isUsed = true;
                            break;
                        }
                    }
                }

                if (isUsed)
                {
                    matchingRecipes.Add(recipe);
                }
            }

            // Sắp xếp các công thức tìm được dựa theo DisplayOrder của vật phẩm đầu ra
            IOrderedEnumerable<CraftingRecipe> orderedRecipes = matchingRecipes.OrderBy(r =>
                BlocksManager.Blocks[Terrain.ExtractContents(r.ResultValue)].GetDisplayOrder(r.ResultValue)
            );

            foreach (CraftingRecipe recipe in orderedRecipes)
            {
                m_recipesList.AddItem(recipe);
            }

            // Cập nhật tiêu đề màn hình
            string blockName = ingredientBlock.GetDisplayName(null, m_ingredientValue);
            m_categoryLabel.Text = $"Crafted from: {blockName} ({matchingRecipes.Count})";
        }
    }
}
