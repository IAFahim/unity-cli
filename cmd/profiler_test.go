package cmd

import "testing"

func TestProfilerCmd_DefaultAction(t *testing.T) {
	send, params := mockSend("manage_profiler", t)
	_, _ = profilerCmd(nil, send)
	if (*params)["action"] != "hierarchy" {
		t.Errorf("expected default action=hierarchy, got %v", (*params)["action"])
	}
}

func TestProfilerCmd_Actions(t *testing.T) {
	for _, action := range []string{"enable", "disable", "status", "clear"} {
		t.Run(action, func(t *testing.T) {
			send, params := mockSend("manage_profiler", t)
			_, _ = profilerCmd([]string{action}, send)
			if (*params)["action"] != action {
				t.Errorf("expected action=%s, got %v", action, (*params)["action"])
			}
		})
	}
}

func TestProfilerCmd_HierarchyFlags(t *testing.T) {
	send, params := mockSend("manage_profiler", t)
	_, _ = profilerCmd([]string{"hierarchy", "--depth", "5", "--min", "0.1", "--sort", "time"}, send)
	if (*params)["depth"] != 5 {
		t.Errorf("expected depth=5, got %v", (*params)["depth"])
	}
	if (*params)["min_time"] != 0.1 {
		t.Errorf("expected min_time=0.1, got %v", (*params)["min_time"])
	}
	if (*params)["sort_by"] != "time" {
		t.Errorf("expected sort_by=time, got %v", (*params)["sort_by"])
	}
}

func TestProfilerCmd_UnknownAction(t *testing.T) {
	send, _ := mockSend("manage_profiler", t)
	_, err := profilerCmd([]string{"explode"}, send)
	if err == nil {
		t.Error("expected error for unknown action")
	}
}
