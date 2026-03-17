package cmd

import (
	"reflect"
	"testing"
)

func TestReserializeCmd_NoArgs(t *testing.T) {
	send, params := mockSend("reserialize_assets", t)
	_, _ = reserializeCmd(nil, send)
	if len(*params) != 0 {
		t.Errorf("expected empty params, got %v", *params)
	}
}

func TestReserializeCmd_SinglePath(t *testing.T) {
	send, params := mockSend("reserialize_assets", t)
	_, _ = reserializeCmd([]string{"Assets/Foo.prefab"}, send)
	if (*params)["path"] != "Assets/Foo.prefab" {
		t.Errorf("expected path=Assets/Foo.prefab, got %v", (*params)["path"])
	}
}

func TestReserializeCmd_MultiplePaths(t *testing.T) {
	send, params := mockSend("reserialize_assets", t)
	paths := []string{"Assets/A.prefab", "Assets/B.prefab", "Assets/C.prefab"}
	_, _ = reserializeCmd(paths, send)
	got, ok := (*params)["paths"].([]string)
	if !ok || !reflect.DeepEqual(got, paths) {
		t.Errorf("paths=%v, want %v", got, paths)
	}
}
