package cmd

import "testing"

func TestMenuCmd_Basic(t *testing.T) {
	send, params := mockSend("execute_menu_item", t)
	_, _ = menuCmd([]string{"File/Save"}, send)
	if (*params)["menu_path"] != "File/Save" {
		t.Errorf("expected menu_path=File/Save, got %v", (*params)["menu_path"])
	}
}

func TestMenuCmd_EmptyArgs(t *testing.T) {
	send, _ := mockSend("execute_menu_item", t)
	_, err := menuCmd(nil, send)
	if err == nil {
		t.Error("expected error for empty args")
	}
}
