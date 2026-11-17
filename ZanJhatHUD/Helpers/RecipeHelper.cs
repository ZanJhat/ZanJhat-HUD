using System.Linq;
using System.Collections.Generic;
using System;
using Engine;
using Game;

namespace ZanJhat.HUD
{
    public static class RecipeHelper
    {
        // --- EVENTS & HOOKS DÀNH CHO CÁC MOD KHÁC ---

        // Cho phép mod khác ghi đè kết quả HasRecipe. 
        public static event Func<int, bool?> OverrideHasRecipe;

        // Kích hoạt TRƯỚC khi mở màn hình Recipe.
        public static event Func<int, bool> OnBeforeOpenRecipeScreen;

        // Cho phép mod khác ghi đè kết quả IsUsedAsIngredient.
        public static event Func<int, bool?> OverrideIsUsedAsIngredient;

        // Kích hoạt TRƯỚC khi mở màn hình IngredientUsage.
        public static event Func<int, bool> OnBeforeOpenIngredientUsageScreen;

        // Kích hoạt SAU khi màn hình đã chuyển thành công.
        public static event Action<string, int> OnAfterScreenSwitched;

        // ------

        // Kiểm tra xem một vật phẩm (dựa vào value) có ít nhất 1 công thức chế tạo nào hay không
        public static bool HasRecipe(int value)
        {
            // Hook: Cho phép ghi đè logic hoặc chặn
            if (OverrideHasRecipe != null)
            {
                // Lấy tất cả các subcribers
                foreach (Func<int, bool?> handler in OverrideHasRecipe.GetInvocationList())
                {
                    bool? result = handler.Invoke(value);
                    if (result.HasValue)
                        return result.Value; // Trả về ngay nếu có mod can thiệp
                }
            }

            // Logic mặc định
            return CraftingRecipesManager.Recipes.Any(r => r.ResultValue == value);
        }

        // Chuyển sang màn hình công thức chế tạo (RecipaediaRecipes) nếu vật phẩm đó có công thức.
        public static void OpenRecipeScreen(int value)
        {
            if (HasRecipe(value))
            {
                // Hook: Chặn mở màn hình. Nếu bất kỳ mod nào trả về false -> Cancel
                if (OnBeforeOpenRecipeScreen != null)
                {
                    foreach (Func<int, bool> handler in OnBeforeOpenRecipeScreen.GetInvocationList())
                    {
                        if (!handler.Invoke(value))
                            return;
                    }
                }

                int contents = Terrain.ExtractContents(value);
                Block block = BlocksManager.Blocks[contents];
                string screenName = "RecipaediaRecipes";

                // Khởi tạo màn hình Recipes thông qua phương thức mặc định của Block
                ScreensManager.m_screens[screenName] = block.GetBlockRecipeScreen(value);

                // Yêu cầu Game chuyển sang màn hình đó, truyền value vào làm tham số (parameter)
                ScreensManager.SwitchScreen(screenName, value);

                // Hook: Thông báo cho các mod khác biết màn hình đã mở xong
                OnAfterScreenSwitched?.Invoke(screenName, value);
            }
        }

        // Kiểm tra xem vật phẩm này có được dùng làm nguyên liệu cho bất kỳ công thức nào không
        public static bool IsUsedAsIngredient(int value)
        {
            // Hook: Cho phép ghi đè logic hoặc chặn
            if (OverrideIsUsedAsIngredient != null)
            {
                foreach (Func<int, bool?> handler in OverrideIsUsedAsIngredient.GetInvocationList())
                {
                    bool? result = handler.Invoke(value);
                    if (result.HasValue)
                        return result.Value;
                }
            }

            // Logic mặc định
            Block ingredientBlock = BlocksManager.Blocks[Terrain.ExtractContents(value)];
            string targetCraftingId = ingredientBlock.CraftingId;
            int targetData = Terrain.ExtractData(value);

            foreach (CraftingRecipe recipe in CraftingRecipesManager.Recipes)
            {
                foreach (string ingredientStr in recipe.Ingredients)
                {
                    if (string.IsNullOrEmpty(ingredientStr)) continue;

                    CraftingRecipesManager.DecodeIngredient(ingredientStr, out string reqId, out int? reqData);

                    // Nếu CraftingId khớp và Data khớp (hoặc công thức không yêu cầu Data cụ thể)
                    if (reqId == targetCraftingId)
                    {
                        if (!reqData.HasValue || reqData.Value == targetData)
                            return true; // Tìm thấy ít nhất 1 công thức thì trả về true luôn
                    }
                }
            }
            return false;
        }

        // Mở màn hình hiển thị danh sách các món đồ có thể chế tạo từ vật phẩm này
        public static void OpenIngredientUsageScreen(int value)
        {
            if (IsUsedAsIngredient(value))
            {
                // Hook: Chặn mở màn hình. Nếu bất kỳ mod nào trả về false -> Cancel
                if (OnBeforeOpenIngredientUsageScreen != null)
                {
                    foreach (Func<int, bool> handler in OnBeforeOpenIngredientUsageScreen.GetInvocationList())
                    {
                        if (!handler.Invoke(value))
                            return;
                    }
                }

                string screenName = "RecipaediaIngredientUsage";

                // Đề phòng trường hợp màn hình chưa được đăng ký trong lúc Load Mod
                if (!ScreensManager.m_screens.ContainsKey(screenName))
                {
                    ScreensManager.AddScreen(screenName, new RecipaediaIngredientUsageScreen());
                }

                // Gọi chuyển màn hình, truyền value của nguyên liệu vào
                ScreensManager.SwitchScreen(screenName, value);

                // Hook: Thông báo cho các mod khác biết màn hình đã mở xong
                OnAfterScreenSwitched?.Invoke(screenName, value);
            }
        }
    }
}
